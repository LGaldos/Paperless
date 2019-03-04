using System;
using Microsoft.SharePoint;
using System.Configuration;
using System.Globalization;

namespace ESMA.Paperless.MaintenanceTasks.v16
{
       
    class PaperlessUsage
    {
        public static string urlWeb = ConfigurationManager.AppSettings["url"];
        public static string pathLOGs = ConfigurationManager.AppSettings["pathLOGS"];
        public static string yearsToAnalyse = ConfigurationManager.AppSettings["years"];
        public static string monthsToAnalyse = ConfigurationManager.AppSettings["months"];

      
        #region <FORMS>

        public static void GetTotalWFsPerWFTypeModule()
        {
            try
            {

                General.TraceHeader("*** Workflows created per workflow types. *** -    Started at: " + System.DateTime.Now.ToString(), ConsoleColor.Green);
                General.CreateFolderXML(pathLOGs);

                if (!string.IsNullOrEmpty(yearsToAnalyse))
                {

                    String[] yearsList = yearsToAnalyse.Split(',');
                    String[] monthList = monthsToAnalyse.Split(',');


                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite colsit = new SPSite(urlWeb))
                        {
                            SPWeb web = colsit.OpenWeb();
                            web.AllowUnsafeUpdates = true;

                            //RS WF Configuration
                            SPListItemCollection itemWFTypeCollection = SP.GetAllWFsTypeInformation(web);


                            if (itemWFTypeCollection.Count > 0)
                            {
                                int cont = 0;
                                int numWFTypes = itemWFTypeCollection.Count;
                                General.TraceInformation("- Total WF Types to review: '" + numWFTypes.ToString() + "'.", ConsoleColor.White);


                                foreach (SPListItem wfTypeItem in itemWFTypeCollection)
                                {
                                    GetWFsUsageByYear(wfTypeItem, ref cont, web, yearsList, monthList);
                                }


                            }
                            else
                                General.TraceInformation("The 'RS Workflow Configuration' list is empty.", ConsoleColor.Red);



                            web.AllowUnsafeUpdates = false;
                            web.Close();
                            web.Dispose();
                        }

                    });

                }
                else
                    General.TraceInformation("No year has been especified to be analysed.", ConsoleColor.Red);
                   
                }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

               
            }

        private static void GetWFsUsageByYear(SPListItem wfTypeItem, ref int cont, SPWeb web, String[] yearsList, String[] monthList)
        {
            try
            {
                string wfType = wfTypeItem["Title"].ToString();
                cont = cont + 1;
                


                if (wfTypeItem["WFLibraryURL"] != null)
                {
                    General.TraceHeader("*** (" + cont + ") - '" + wfType + "'.", ConsoleColor.Green);
                    
                    SPList wfLibraryList = SP.GetWFLibrary(web, wfTypeItem);
                    //Total WFs
                    int numWFs = SP.GetTotalItems(web, wfLibraryList);
                    General.TraceInformation("- Total WFs to review: '" + numWFs.ToString() + "'.", ConsoleColor.Gray);

                    if (numWFs > 0)
                    {

                        foreach (var year in yearsList)
                        {
                            try
                            {
                               GetWFsUsageByMonth(monthList, web, wfLibraryList,  year);
                            }
                            catch
                            {
                                General.TraceInformation("*** Error getting WFs Total in the WFLibrary: '" + wfLibraryList.Title + " -> Year: '" + year + "'", ConsoleColor.Red);
                                continue;
                            }
                        }

                       
                    }


                }
                else
                    General.TraceInformation("- WFType: '" + wfType + "' does not have 'WFLibraryURL'.", ConsoleColor.Red);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static void GetWFsUsageByMonth(String[] monthList, SPWeb web, SPList wfLibraryList, string year)
        {
            try
            {
           
                foreach (var month in monthList)
                {
                    try
                    {
                        int totalWFs =  SP.GetItemsFromADate(web, wfLibraryList, year, month);
                        General.TraceInformation("- " + year + "/" + month + "-> WFs Total: " + totalWFs.ToString() + "'.", ConsoleColor.White);
                    }
                    catch
                    {
                        General.TraceInformation("*** Error getting WFs Total in the WFLibrary: '" + wfLibraryList.Title + " -> Year: '" + year + "' + Month: '" + month +  "'", ConsoleColor.Red);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        #endregion

        #region <LOGS>

        public static void GetTotalLogsPerWFTypeModule()
        {
             try
            {

                General.TraceHeader("*** Logs created per workflow types. *** -    Started at: " + System.DateTime.Now.ToString(), ConsoleColor.Green);
                General.CreateFolderXML(pathLOGs);

                if (!string.IsNullOrEmpty(yearsToAnalyse))
                {

                    String[] yearsList = yearsToAnalyse.Split(',');
                    String[] monthList = monthsToAnalyse.Split(',');


                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite colsit = new SPSite(urlWeb))
                        {
                            SPWeb web = colsit.OpenWeb();
                            web.AllowUnsafeUpdates = true;

                            //RS WF Configuration
                            SPListItemCollection itemWFTypeCollection = SP.GetAllWFsTypeInformation(web);
                           

                            if (itemWFTypeCollection.Count > 0)
                            {
                                int cont = 0;
                                int numWFTypes = itemWFTypeCollection.Count;
                         
                                General.TraceInformation("- Total WF Types to review: '" + numWFTypes.ToString() + "'.", ConsoleColor.White);


                                foreach (SPListItem wfTypeItem in itemWFTypeCollection)
                                {
                                    GetLogsUsageByYear(wfTypeItem, ref cont, web, yearsList, monthList);
                                }


                            }
                            else
                                General.TraceInformation("The 'RS Workflow Configuration' list is empty.", ConsoleColor.Red);



                            web.AllowUnsafeUpdates = false;
                            web.Close();
                            web.Dispose();
                        }

                    });

                }
                else
                    General.TraceInformation("No year has been especified for being analysed.", ConsoleColor.Red);
                   
                }
             catch (Exception ex)
             {
                 General.TraceException(ex);
             }
        }

        private static void GetLogsUsageByYear(SPListItem wfTypeItem, ref int cont, SPWeb web, String[] yearsList, String[] monthList)
        {
            try
            {
                string wfType = wfTypeItem["Title"].ToString();
                cont = cont + 1;



                if (wfTypeItem["WFLibraryURL"] != null)
                {
                    General.TraceHeader("*** (" + cont + ") - '" + wfType + "'.", ConsoleColor.Green);

                    SPList logsList = SP.GetWFLogList(web, wfTypeItem);
                    //Total WFs
                    int totalLogs = SP.GetTotalItems(web, logsList);
                    General.TraceInformation("- Logs Total to review: '" + totalLogs.ToString() + "'.", ConsoleColor.Gray);

                    if (totalLogs > 0)
                    {

                        foreach (var year in yearsList)
                        {
                            try
                            {
                                GetLogsUsageByMonth(monthList, web, logsList, year);
                            }
                            catch
                            {
                                General.TraceInformation("*** Error getting Logs Total in the WFLibrary: '" + logsList.Title + " -> Year: '" + year + "'", ConsoleColor.Red);
                                continue;
                            }
                        }


                    }


                }
                else
                    General.TraceInformation("- WFType: '" + wfType + "' does not have 'WFLogsURL'.", ConsoleColor.Red);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static void GetLogsUsageByMonth(String[] monthList, SPWeb web, SPList logsList, string year)
        {
            try
            {
                
                foreach (var month in monthList)
                {
                    try
                    {
                        int totalLogs =  SP.GetItemsFromADate(web, logsList, year, month);
                        General.TraceInformation("- " + year + "/" + month + "-> Logs Total: " + totalLogs.ToString() + "'.", ConsoleColor.White);
                    }
                    catch
                    {
                        General.TraceInformation("*** Error getting Logs Total in the WFLibrary: '" + logsList.Title + " -> Year: '" + year + "' + Month: '" + month + "'", ConsoleColor.Red);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        #endregion

        #region <DOCUMENTS>

        public static void GetTotalDocumentsPerWFTypeModule()
        {
            try
            {

                General.TraceHeader("*** Attachments per workflow types. *** -    Started at: " + System.DateTime.Now.ToString(), ConsoleColor.Green);
                General.CreateFolderXML(pathLOGs);

                if (!string.IsNullOrEmpty(yearsToAnalyse))
                {

                    String[] yearsList = yearsToAnalyse.Split(',');
                    String[] monthList = monthsToAnalyse.Split(',');


                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite colsit = new SPSite(urlWeb))
                        {
                            SPWeb web = colsit.OpenWeb();
                            web.AllowUnsafeUpdates = true;

                            //RS WF Configuration
                            SPListItemCollection itemWFTypeCollection = SP.GetAllWFsTypeInformation(web);

                            if (itemWFTypeCollection.Count > 0)
                            {
                                int cont = 0;
                                int numWFTypes = itemWFTypeCollection.Count;
                                General.TraceInformation("- Total WF Types to review: '" + numWFTypes.ToString() + "'.", ConsoleColor.White);


                                foreach (SPListItem wfTypeItem in itemWFTypeCollection)
                                {
                                    GetAttachedDocsUsageByYear(wfTypeItem, ref cont, web, yearsList, monthList);
                                }


                            }
                            else
                                General.TraceInformation("The 'RS Workflow Configuration' list is empty.", ConsoleColor.Red);



                            web.AllowUnsafeUpdates = false;
                            web.Close();
                            web.Dispose();
                        }

                    });

                }
                else
                    General.TraceInformation("No year has been especified for being analysed.", ConsoleColor.Red);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }


        }

        private static void GetAttachedDocsUsageByYear(SPListItem wfTypeItem, ref int cont, SPWeb web, String[] yearsList, String[] monthList)
        {
            try
            {
                string wfType = wfTypeItem["Title"].ToString();
                cont = cont + 1;



                if (wfTypeItem["WFLibraryURL"] != null)
                {
                    General.TraceHeader("*** (" + cont + ") - '" + wfType + "'.", ConsoleColor.Green);

                    SPList wfLibraryList = SP.GetWFLibrary(web, wfTypeItem);
                    //Total WFs
                    int numDocuments = SP.GetTotalAttachedDocuments(web, wfLibraryList);
                    General.TraceInformation("- Total Attachments to review: '" + numDocuments.ToString() + "'.", ConsoleColor.Gray);

                    if (numDocuments > 0)
                    {

                        foreach (var year in yearsList)
                        {
                            try
                            {
                                GetAttachedDocsUsageByMonth(monthList, web, wfLibraryList, year);
                            }
                            catch
                            {
                                General.TraceInformation("*** Error getting the total of attachments in the WFLibrary: '" + wfLibraryList.Title + " -> Year: '" + year + "'", ConsoleColor.Red);
                                continue;
                            }
                        }


                    }


                }
                else
                    General.TraceInformation("- WFType: '" + wfType + "' does not have 'WFLibraryURL'.", ConsoleColor.Red);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static void GetAttachedDocsUsageByMonth(String[] monthList, SPWeb web, SPList wfLibraryList, string year)
        {
            try
            {
                foreach (var month in monthList)
                {
                    
                    try
                    {
                        int totalAttachments = SP.GetAttachedDocumentsFromADate(web, wfLibraryList, year, month);
                        General.TraceInformation("- " + year + "/" + month + "-> Attachments Total: " + totalAttachments.ToString() + "'.", ConsoleColor.White);
                    }
                    catch
                    {
                        General.TraceInformation("*** Error getting the total of attachments in the WFLibrary: '" + wfLibraryList.Title + " -> Year: '" + year + "' + Month: '" + month + "'", ConsoleColor.Red);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        #endregion


    }
}
