namespace ViewTelegramBot.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AccessesAttribute : Attribute
{
    public long[] Accesses { get; }

    // public AccessesAttribute(params Access[] accesses) => Accesses = accesses.Select(x => (long)x).ToArray();
}