using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using System.Text;
using System.Web;
using System.ComponentModel;
using System.Data;
using System.Collections.Generic;

namespace ESMA.Paperless.FileUploader.v15.RSWorkflowFileUploader
{
    public partial class RSWorkflowFileUploaderUserControl : UserControl
    {
        Dictionary<string, string> parameters;

       

        protected override void OnPreRender(EventArgs e)
        {
            try
            {
                
                StringBuilder initParamms = new StringBuilder();

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb Web = Site.OpenWeb();
                        string url = HttpContext.Current.Request.Url.ToString();

                        string wfid = HttpContext.Current.Request.QueryString["wfid"];
                        string wftypeOrder = HttpContext.Current.Request.QueryString["wftype"];
                        string wfdoctype = HttpContext.Current.Request.QueryString["wfdoctype"];


                        if (!string.IsNullOrEmpty(wfid) && !string.IsNullOrEmpty(wftypeOrder) && !string.IsNullOrEmpty(wfdoctype))
                        {
                            parameters = Methods.GetConfigurationParameters(Web);
                            string XapLocation = Web.Url + parameters["Silverlight Visor - FileLocation"];
                            string wftypeName = Methods.GetWorkflowTypeName(wftypeOrder, Web);
                            string wfLibraryURL = Methods.GetWorkflowLibraryURL(wftypeName, Web);
                            SPList wfLibrary = Methods.GetWorkflowLibrary(wfLibraryURL, Web);

                            Methods.GetSilverlightVisorParameters(ref initParamms, Web, wfLibraryURL, wfdoctype, parameters, wfid, wfLibrary.Title);

                            string renderHost =
                               @"<div id='silverlightControlHost'>     
                        <object data='data:application/x-silverlight-2,' type='application/x-silverlight-2' width='720' height='420'>
                            <param name='source' value='" + XapLocation + @"'/>     
                            <param name='background' value='white' />     
                            <param name='minRuntimeVersion' value='4.0.50303.0' />     
                            <param name='autoUpgrade' value='true' />     
                            <param name='windowless' value='false'/>     
                            <param name='initParams' value='" + initParamms.ToString() + @"' />     
                            <a href='http://go.microsoft.com/fwlink/?LinkID=149156&v=4.0.50303.0' style='text-decoration:none'>     
                            <img src='http://go.microsoft.com/fwlink/?LinkId=161376' alt='Get Microsoft Silverlight' style='border-style:none'/></a>     
                        </object>
                        <iframe id='_sl_historyFrame' style='visibility:hidden; height:0px;width:0px;border:0px'></iframe>
                     </div>";

                            LiteralControl host = new LiteralControl(renderHost);
                            Controls.Add(host);
                            base.OnPreRender(e);


                            Web.Close();
                            Web.Dispose();
                        }
                    }
                });

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(string.Empty, "OnPreRender()" + ex.Message);
            }
        }

       

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
