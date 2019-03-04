using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Text;

namespace ESMA.Paperless.Webparts.v16.RSWorkflowViewToBeSigned
{
    [ToolboxItemAttribute(false)]
    public class RSWorkflowViewToBeSigned : WebPart
    {
        // Visual Studio might automatically update this path when you change the Visual Web Part project item.
        private const string _ascxPath = @"~/_CONTROLTEMPLATES/15/ESMA.Paperless.Webparts.v16/RSWorkflowViewToBeSigned/RSWorkflowViewToBeSignedUserControl.ascx";

        protected override void CreateChildControls()
        {
            Control control = Page.LoadControl(_ascxPath);
            Controls.Add(control);
        }

        public override void RenderControl(HtmlTextWriter writer)
        {
            try
            {
                StringBuilder sbStyles = new StringBuilder();
                sbStyles.Append("<link id=\"LinkStyles\" rel=\"stylesheet\" type=\"text/css\" href=\"/_layouts/15/ESMA.Paperless.Design.v16/css/RSGridStyles.css\"></link>");
                sbStyles.Append("<link id=\"LinkComunStyles\" rel=\"stylesheet\" type=\"text/css\" href=\"/_layouts/15/ESMA.Paperless.Design.v16/css/RSComun.css\"></link>");
                writer.Write(sbStyles.ToString());
                writer.Write(sbStyles.ToString());
                base.RenderControl(writer);
            }
            catch { }
        }
    }
}
