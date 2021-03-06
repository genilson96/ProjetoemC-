using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Net;
using System.Linq;
using System.ServiceModel.Description;
using TreinamentoExtending;
//using ConexaoAlternativaExtending;

namespace TreinamentoExtending
{
    class Program
    {
        static void Main(string[] args)
        {
            //Descoberta();
            var serviceproxy = new Conexao().Obter();

            queryFetchXML(serviceproxy);
            // MeuCreate(serviceproxy);
            // RetornarMultiplo(serviceproxy);
            //RetornarMultiploLinkEntity(serviceproxy);
            // ConsultaLinq(serviceproxy);
            //CriarContaLinq(serviceproxy);
            //UpdateContaLinq(serviceproxy);
            //ExcluirContaLinq(serviceproxy);
            //queryFetchXMLAgregate(serviceproxy);
            //MascaraCpf(serviceproxy);
        }

        #region QueryFetchXML

        static void queryFetchXML(CrmServiceClient serviceproxy)
        {
            String fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='account'>
                    <attribute name='name' />
                    <attribute name='address1_city' />
                    <attribute name='primarycontactid' />
                    <attribute name='telephone1' />
                    <attribute name='defaultpricelevelid' />
                    <attribute name='accountid' />
                    <order attribute='name' descending='false' />
                    <filter type='and'>
                      <condition attribute='name' operator='eq' value='' />
                      <condition attribute='emailaddress1' operator='eq' value='' />
                      <condition attribute='telephone1' operator='eq' value='' />
                      <condition attribute='new_cpf2' operator='eq' value='' />
                      <condition attribute='new_cnpj2' operator='eq' value='' />
                      <condition attribute='primarycontactid' operator='eq' />
                    </filter>
                    <link-entity name='contact' from='contactid' to='primarycontactid' visible='false' link-type='outer' alias='accountprimarycontactidcontactcontactid'>
                      <attribute name='emailaddress1' />
                    </link-entity>
                  </entity>
                </fetch>";
            EntityCollection rsContas = serviceproxy.RetrieveMultiple(new FetchExpression(fetchXML));

            foreach (var registro in rsContas.Entities)
            {
                Console.WriteLine(registro["primarycontactid"].ToString() + " - " + registro["name"]);
            }
            Console.ReadKey();
        }
        static void queryFetchXMLAgregate(CrmServiceClient serviceProxy)
        {
            string fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' aggregate='true'>
                                   <entity name='account'>
                                    <attribute name='creditlimit' alias='creditlimit_soma' aggregate='sum'/>
                                   </entity>
                                 </fetch>";

            EntityCollection colecaoEntidades = serviceProxy.RetrieveMultiple(new FetchExpression(fetchXML));
            
            foreach(var registro in colecaoEntidades.Entities)
            {
                if (registro.Attributes.Contains("creditlimit_soma"))
                {
                    Console.WriteLine("Limite de Credito Total: " + ((Money)((AliasedValue)registro["creditlimit_soma"]).Value).Value.ToString());
                }
                else
                {
                    Console.WriteLine("Limite de Credito Não Informado!");
                }
            }
            Console.ReadKey();
        }
        #endregion

        #region Metadados
        static void MeuCreate(CrmServiceClient serviceProxy)
        {
            for (int i = 1; i < 10; i++)
            {
                var entityAccount = new Entity("account");
                Guid registroID = new Guid();

                entityAccount.Attributes.Add("name", " Meu registro " + i.ToString());

                registroID = serviceProxy.Create(entityAccount);

                Console.WriteLine("Registro:" + registroID.ToString() + " - criado com sucesso!");
            }
            Console.ReadKey();
        }
        #endregion

        #region QueryExpression
        static EntityCollection RetornarMultiplo(CrmServiceClient serviceProxy)
        {
            QueryExpression queryExpression = new QueryExpression("account");
            queryExpression.Criteria.AddCondition("name", ConditionOperator.NotNull);
            queryExpression.ColumnSet = new ColumnSet("name");
            EntityCollection colecaoEntidade = serviceProxy.RetrieveMultiple(queryExpression);

            int registro = 0;
            foreach (var item in colecaoEntidade.Entities)
            {
                Console.WriteLine("Registro: " + registro.ToString() + " -- Nome da Conta" + item["name"] + " -- Telefone" + item["Telephone1"]);
                registro++;
            }
            Console.ReadKey();

            return colecaoEntidade;
        }
        static EntityCollection RetornarMultiploLinkEntity(CrmServiceClient serviceProxy)
        {
            QueryExpression queryExpression = new QueryExpression("account");
            queryExpression.Criteria.AddCondition("name", ConditionOperator.NotNull);
            queryExpression.ColumnSet = new ColumnSet(true);

            LinkEntity link = new LinkEntity("account", "contact", "primarycontactid", "contactid", JoinOperator.Inner);
            link.Columns = new ColumnSet("firstname", "lastname");
            link.EntityAlias = "Contato";
            queryExpression.LinkEntities.Add(link);

            EntityCollection colecaoEntidade = serviceProxy.RetrieveMultiple(queryExpression);

            int registro = 0;
            foreach (var item in colecaoEntidade.Entities)
            {
                Console.WriteLine("Registro: " + registro.ToString() + " -- Nome da Conta" + item["name"] + " -- Telefone" + item["Telephone1"]);
                Console.WriteLine("Nome do Contato:" + ((AliasedValue)item["Contato.firstname"]));
                Console.WriteLine("Sobrenome do Contato" + ((AliasedValue)item["Contato.lastname"]));
                registro++;
            }
            Console.ReadKey();

            return colecaoEntidade;
        }
        #endregion
        #region linq
        static void ConsultaLinq(CrmServiceClient serviceProxy)
        {
            OrganizationServiceContext contexto = new OrganizationServiceContext(serviceProxy);
            var result = from cliente in contexto.CreateQuery("account")
                         join contato in contexto.CreateQuery("contact")
                         on cliente["primarycontactid"] equals contato["contactid"]
                         select new
                         {
                             retorno = new
                             {
                                 NomeCliente = cliente["name"],
                                 NomeContato = contato["firstname"]
                             }
                         };
            int registro = 0;
            foreach (var itemlinq in result)
            {
                Console.WriteLine("Registro" + registro.ToString() + " -- Nome do Cliente: "
                    + itemlinq.retorno.NomeCliente + " -- Nome do Contato" + itemlinq.retorno.NomeContato);
                registro++;
            }
            Console.ReadKey();
        }
        static void CriarContaLinq(CrmServiceClient seviceProxy)
        {
            OrganizationServiceContext context = new OrganizationServiceContext(seviceProxy);
            for (int i = 0; i < 10; i++)
            {


                Entity cliente = new Entity("account");
                cliente["name"] = "Luis Prado Linq" + i.ToString();
                context.AddObject(cliente);
                Console.WriteLine("Registro criado" + i.ToString());
            }
            context.SaveChanges();
            Console.ReadKey();
        }

        static void UpdateContaLinq(CrmServiceClient serviceProxy)
        {
            OrganizationServiceContext contexto = new OrganizationServiceContext(serviceProxy);
            var result = from Cliente in contexto.CreateQuery("account")
                         where ((string)Cliente["name"]) == "Luis Prado Linq"
                         select Cliente;

            foreach (var ClienteUpdate in result)
            {
                ClienteUpdate.Attributes["name"] = "Luis Prado Update Linq";
                contexto.UpdateObject(ClienteUpdate);
            }
            contexto.SaveChanges();
        }

        static void ExcluirContaLinq(CrmServiceClient serviceProxy)
        {
            OrganizationServiceContext contexto = new OrganizationServiceContext(serviceProxy);

            var result = from Cliente in contexto.CreateQuery("account")
                         where ((string)Cliente["name"] == "Luis Prado Update Linq")
                         select Cliente;

            foreach (var ClienteDelete in result)
            {
                contexto.DeleteObject(ClienteDelete);
            }
            contexto.SaveChanges();
        }
        #endregion
       
         

        

        
        
      
            
       
        

       
    }
}
    
