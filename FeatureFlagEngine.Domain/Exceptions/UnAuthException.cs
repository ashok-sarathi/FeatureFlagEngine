using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Domain.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class UnAuthException(string message) : Exception(message)
    {
    }
}
