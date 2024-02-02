namespace Brazil.EFCore.Extensions.Tests.BulkInser;

public class Cliente
{
    public int IdCliente { get; set; }
    public string Documento { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string Telefone { get; set; }
    public DateTime DataCadastro { get; set; }

}

public class ClientePF : Cliente
{
    public string Logradouro { get; set; }
    public string NumeroLogradouro { get; set; }
    public string ComplementoLogradouro { get; set; }
    public string Bairro { get; set; }
    public string Cidade { get; set; }
    public string UF { get; set; }
}
