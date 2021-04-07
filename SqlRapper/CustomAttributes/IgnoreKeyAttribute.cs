using System;
using System.Collections.Generic;
using System.Text;

namespace SqlRapper.CustomAttributes
{
    /// <summary>
    /// This attribute can be put over any property name.  It designates a property that should be ignored by the rapper when inserting and updating data.
    /// For more information see the SqlDataService.  Trumps primary key.
    /// </summary>
    public class IgnoreKeyAttribute : Attribute
    {
    }
}
