using Microsoft.EntityFrameworkCore;

namespace Brazil.EFCore.Extensions.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private DataContext _dc;

        [TestInitialize]
        public void Start()
        {
            var connectionString = "Data Source=(LocalDb)\\MSSQLLocalDb;Connection Timeout=90000;Initial Catalog=Agenda;Trusted_Connection=True;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder();
            options.UseSqlServer(connectionString);
            _dc = new DataContext(options.Options);
            _dc.Database.EnsureDeleted();
            _dc.Database.EnsureCreated();
        }

        [TestMethod]
        public void EntityTest()
        {
            //var infrastructure = _dc.ChangeTracker.GetInfrastructure();

            //var entity = new BulkInser.ClientePF() { Documento = "99999999", Nome = $"José da Silva", Email = $"ze@gmail.com", Telefone = "+55 11 99999-9999",
            //    Logradouro = "Rua Central", NumeroLogradouro = "nº 1", ComplementoLogradouro = "Ap 01 Bl 02", Bairro = "Centro", Cidade = "São Paulo", UF = "SP", };
            //var entityType = _dc.Model.FindRuntimeEntityType(entity.GetType());

            //foreach (var property in entityType.GetProperties())
            //{
            //    if (property.IsShadowProperty())
            //    {
            //        var anotacao = entityType.GetAnnotation("Relational:DiscriminatorValue");

            //        foreach(var item in property.GetAnnotations())
            //        {
                        
            //        }

            //        var an = entityType.GetAnnotations();

            //        foreach(var a in an)
            //        {
            //            var valor = a.Value;
            //        }
            //    }
            //}            

            //var entry = infrastructure.GetOrCreateEntry(entity, entityType);
            //var entityType = context.Model.FindRuntimeEntityType(entity.GetType());
            //var infrastructure = context.ChangeTracker.GetInfrastructure();
            //var entry = infrastructure.GetOrCreateEntry(entity, entityType);
        }

        [TestMethod]
        public void ClienteTest()
        {
            int qtde = 1_000_000;

            var lista = new List<BulkInser.Cliente>();
            for (int i = 1; i <= qtde; i++)
            {
                lista.Add(new BulkInser.ClientePF()
                {
                    Documento = i.ToString("00000000000"),
                    Nome = $"José da Silva",
                    Email = $"ze@gmail.com",
                    Telefone = "+55 11 99999-9999",
                    Logradouro = "Rua Central",
                    NumeroLogradouro = "nº " + i,
                    ComplementoLogradouro = "Ap 01 Bl 02",
                    Bairro = "Centro",
                    Cidade = "São Paulo",
                    UF = "SP",
                    DataCadastro = DateTime.Now,
                });
            }

            Console.WriteLine("Inserindo " + qtde.ToString("###,###,###"));
            var data = DateTime.Now;
            _dc.BulkInsert(lista, 100000);
            var tempo = DateTime.Now.Subtract(data);
            Console.WriteLine("Tempo: " + tempo);
            Console.WriteLine();
            Console.WriteLine();

            //Console.WriteLine("Consulta Tipada " + qtde.ToString("###,###,###"));
            //data = DateTime.Now;
            //var lista1 = _dc.Select<BulkInser.Cliente>("select *  from Clientes");
            //Console.WriteLine("Tempo: " + DateTime.Now.Subtract(data));
            //Console.WriteLine();
            //Console.WriteLine();

            //Console.WriteLine("Consulta " + qtde.ToString("###,###,###"));
            //data = DateTime.Now;
            //var lista2 = _dc.Select("select * from Clientes");
            //Console.WriteLine("Tempo: " + DateTime.Now.Subtract(data));
            //Console.WriteLine();
            //Console.WriteLine();
        }
    }
}
