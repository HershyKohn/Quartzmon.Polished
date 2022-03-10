using System.Collections.Generic;

namespace Quartzmon.Models
{
    public interface IHasValidation
    {
        void Validate(ICollection<ValidationError> errors);
    }
}