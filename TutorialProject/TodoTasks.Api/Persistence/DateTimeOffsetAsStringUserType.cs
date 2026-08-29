namespace TodoTasks.Api.Persistence;

using System.Data.Common;
using System.Globalization;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

/// <summary>
/// Maps a <see cref="DateTimeOffset"/> column to a round-trippable ISO 8601 string, since SQLite's
/// NHibernate dialect has no native mapping for <see cref="System.Data.DbType.DateTimeOffset"/> at
/// all - <c>TodoTaskMap</c> mapping <see cref="Domain.TodoTask.CreatedAtUtc"/> without this throws
/// <c>ArgumentException: Dialect does not support DbType.DateTimeOffset</c> the moment the schema
/// exports, confirmed against the real error, not assumed. SQLite has no native
/// timestamp-with-offset type either, so storing as a sortable, round-trippable string (the
/// <c>"o"</c>/round-trip format specifier) is the standard workaround for this exact combination.
/// </summary>
public sealed class DateTimeOffsetAsStringUserType : IUserType
{
    /// <inheritdoc/>
    public SqlType[] SqlTypes { get; } = [SqlTypeFactory.GetString(64)];

    /// <inheritdoc/>
    public Type ReturnedType => typeof(DateTimeOffset);

    /// <inheritdoc/>
    public bool IsMutable => false;

    /// <inheritdoc/>
    // object.Equals(...) explicitly, not a bare Equals(x, y) call: the latter resolves back to this
    // very method (an unqualified two-argument call still matches this instance method's own
    // signature once the arguments are boxed to object), recursing until the stack overflows -
    // reproduced and confirmed, not a hypothetical.
    public new bool Equals(object? x, object? y) => object.Equals(x, y);

    /// <inheritdoc/>
    public int GetHashCode(object x) => x.GetHashCode();

    /// <inheritdoc/>
    public object? NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var value = NHibernateUtil.String.NullSafeGet(rs, names[0], session);
        return value is string text ? DateTimeOffset.ParseExact(text, "o", CultureInfo.InvariantCulture) : null;
    }

    /// <inheritdoc/>
    public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
    {
        var text = value is DateTimeOffset dto ? dto.ToString("o", CultureInfo.InvariantCulture) : null;
        NHibernateUtil.String.NullSafeSet(cmd, text, index, session);
    }

    /// <inheritdoc/>
    public object DeepCopy(object value) => value;

    /// <inheritdoc/>
    public object Replace(object original, object target, object owner) => original;

    /// <inheritdoc/>
    public object Assemble(object cached, object owner) => cached;

    /// <inheritdoc/>
    public object Disassemble(object value) => value;
}
