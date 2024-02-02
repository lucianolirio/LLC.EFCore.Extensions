using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;
using System.Security.AccessControl;

namespace Brazil.EFCore.Extensions.Tests.BulkInser;

public class ClienteMapping : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(p => p.IdCliente);

        builder.Property(p => p.IdCliente).ValueGeneratedOnAdd();
        builder.Property(p => p.Documento);
        builder.Property(p => p.Nome).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Telefone).HasMaxLength(20);
        builder.Property(p => p.DataCadastro).IsRequired();

        builder.HasDiscriminator<string>("DiscriminatorColumn")
            .HasValue<Cliente>("Cliente")
            .HasValue<ClientePF>("ClientePF");

        //builder.HasIndex(p => p.Documento).IsUnique().HasName("IX_Clientes_Documento");
        //builder.HasIndex(p => p.Email).IsUnique().HasName("IX_Clientes_Email");

        builder.ToTable("ClientesTeste");
    }
}

public class ClientePFMapping : IEntityTypeConfiguration<ClientePF>
{
    public void Configure(EntityTypeBuilder<ClientePF> builder)
    {
        builder.Property(p => p.Logradouro).HasMaxLength(100);
        builder.Property(p => p.NumeroLogradouro).HasMaxLength(20);
        builder.Property(p => p.ComplementoLogradouro).HasMaxLength(50);
        builder.Property(p => p.Bairro).HasMaxLength(80);
        builder.Property(p => p.Cidade).HasMaxLength(80);
        builder.Property(p => p.UF).HasMaxLength(2).HasColumnType("char(2)");

        //builder.HasDiscriminator<int>("Tipo").HasValue(10000);
    }
}
