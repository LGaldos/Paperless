using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;
using System.Text;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReportsMenu
{
    [ToolboxItemAttribute(false)]
    public class RSWorkflowReportsMenu : WebPart
    {
        // Visual Studio might automatically update this path when you change the Visual Web Part project item.
        private const string _ascxPath = @"~/_CONTROLTEMPLATES/15/ESMA.Paperless.Reports.v16/RSWorkflowReportsMenu/RSWorkflowReportsMenuUserControl.ascx";

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

                sbStyles.Append("<link id=\"LinkStyles\" rel=\"stylesheet\" type=\"text/css\" href=\"/_layouts/15/ESMA.Paperless.Design.v16/css/RSComun.css\"></link>");
                sbStyles.Append("<link id=\"LinkStyles\" rel=\"stylesheet\" type=\"text/css\" href=\"/_layouts/15/ESMA.Paperless.Design.v16/css/RSReportsStyles.css\"></link>");
                writer.Write(sbStyles.ToString());
                base.RenderControl(writer);
            }
            catch { }
        }

    }
}
