using DevToys.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnifeSQLExtension.UI.Views
{
    public interface IView
    {
        IUIElement View { get; }
    }
}
