using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Domain.Exceptions
{
    public class BadRequestException(string message) : Exception(message)
    {
    }
}
