using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Text;

namespace ESMA.Paperless.Webparts.v16.RSWorkflowInitiation
{
    [ToolboxItemAttribute(false)]
    public class RSWorkflowInitiation : WebPart
    {
        // Visual Studio might automatically update this path when you change the Visual Web Part project item.
        private const string _ascxPath = @"~/_CONTROLTEMPLATES/15/ESMA.Paperless.Webparts.v16/RSWorkflowInitiation/RSWorkflowInitiationUserControl.ascx";

        protected override void CreateChildControls()
        {
            Control control = Page.LoadControl(_ascxPath);
            Controls.Add(control);
        }

        public override void RenderControl(HtmlTextWriter writer)
        {
            try
            {
                if (HttpContext.Current.Session["FormCrashOnLoad"] == null)
                {
                    StringBuilder sbStyles = new StringBuilder();
                    sbStyles.Append("<link id=\"LinkComunStyles\" rel=\"stylesheet\" type=\"text/css\" href=\"/_layouts/15/ESMA.Paperless.Design.v16/css/RSComun.css\"></link>");
                    writer.Write(sbStyles.ToString());
                    base.RenderControl(writer);
                }
            }
            catch { }
        }
    }
}
