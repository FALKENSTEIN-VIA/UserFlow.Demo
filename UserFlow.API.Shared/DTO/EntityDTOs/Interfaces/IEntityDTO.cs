using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserFlow.API.Shared.DTO;

public interface IEntityDTO<TId>
{
    TId Id { get; set; }
}