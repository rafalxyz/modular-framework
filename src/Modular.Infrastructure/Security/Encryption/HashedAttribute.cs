using System;

namespace Modular.Infrastructure.Security.Encryption
{
    [AttributeUsage(AttributeTargets.Property)]
    public class HashedAttribute : Attribute
    {
    }
}