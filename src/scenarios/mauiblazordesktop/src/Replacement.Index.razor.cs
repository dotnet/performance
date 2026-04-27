using Microsoft.AspNetCore.Components;
using System.Diagnostics.Tracing;
namespace MauiBlazorDesktopTesting.Pages
{
    public partial class Index
    {
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender) {
                 // Startup logging is added here via the setup script.
            }
        }
    }
}