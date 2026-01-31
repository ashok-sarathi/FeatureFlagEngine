using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FeatureFlagEngine.Domain.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class BadRequestException(string message) : Exception(message)
    {
    }
}
