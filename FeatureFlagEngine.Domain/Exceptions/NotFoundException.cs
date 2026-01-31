using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureFlagEngine.Domain.Exceptions
{
    public class NotFoundException(string message) : Exception(message)
    {
    }
}
