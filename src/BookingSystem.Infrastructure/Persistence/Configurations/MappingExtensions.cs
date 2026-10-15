using BookingSystem.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Persistence.Configurations;

internal static class MappingExtensions
{
    /// <summary>
    /// Name of the shadow property used as optimistic concurrency token.
    /// </summary>
    public const string RowVersionProperty = "RowVersion";

    private const int CurrencyCodeLength = 3;

    /// <summary>
    /// Uses PostgreSQL's <c>xmin</c> system column as concurrency token. Npgsql maps a <see cref="uint"/>
    /// row-version property to <c>xmin</c> and never emits it in migrations. A shadow property keeps
    /// the domain model free of persistence concerns.
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasXminRowVersion<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<uint>(RowVersionProperty)
            .IsRowVersion()
            .HasColumnName("xmin");

        return builder;
    }

    /// <summary>
    /// Maps an owned <see cref="Money"/> to <c>{prefix}_amount numeric(18,2)</c> and <c>{prefix}_currency char(3)</c>.
    /// </summary>
    public static OwnedNavigationBuilder<TOwner, Money> MapMoney<TOwner>(
        this OwnedNavigationBuilder<TOwner, Money> money,
        string columnPrefix)
        where TOwner : class
    {
        money.Property(value => value.Amount)
            .HasColumnName($"{columnPrefix}_amount")
            .HasPrecision(18, 2);

        money.Property(value => value.Currency)
            .HasColumnName($"{columnPrefix}_currency")
            .HasMaxLength(CurrencyCodeLength)
            .IsFixedLength();

        return money;
    }
}
