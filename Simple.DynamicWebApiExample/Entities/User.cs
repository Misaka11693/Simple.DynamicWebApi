using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.DynamicWebApiExample.Entities;

public class User
{
    /// <summary>
    /// User Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User Name
    /// </summary>
    public string? Name { get; set; }
}
