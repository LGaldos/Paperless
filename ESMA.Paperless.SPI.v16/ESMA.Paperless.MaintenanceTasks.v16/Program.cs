using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Data;
using System.Configuration;

namespace ESMA.Paperless.MaintenanceTasks.v16
{
    class Program
    {
        static int iErrores = 0;
        public static DateTime startedTime;

        //--------------------------------------------------------------------
        //Application: ESMA.Paperless.MaintenanceTasks.v16
        //Compatible: SharePoint 2016
        //Build Platform target: x86
        //Framework: .NET Framework 4.5
        //Release: v.2.0.0
        //Modified Date: 23/11/2018
        //--------------------------------------------------------------------

        static void Main(string[] args)
        {

            DateTime startedTime = DateTime.Now;
            string option = string.Empty;
      


            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("==============================================================");
                Console.WriteLine("STARTING PROCESS: " + Convert.ToString(startedTime));
                Console.WriteLine("==============================================================");
                Console.WriteLine("");
                Console.WriteLine("--------------------------------------------------------------");
                Console.WriteLine("Select option: ");
                Console.WriteLine("--------------------------------------------------------------");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("***** SHAREPOINT 2013 *****");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("------------------------------------");
                Console.WriteLine("PAPERLESS USAGE");
                Console.WriteLine("------------------------------------");
                Console.WriteLine("(1) - Paperless Routing Slip II usage (monthly) -> (Opt. 2, 3 and 4)");
                Console.WriteLine("(2) - Workflows created per workflow types.");
                Console.WriteLine("(3) - Number of documents per workflow types.");
                Console.WriteLine("(4) - Number of logs per workflow types.");
                Console.WriteLine();
                Console.WriteLine("------------------------------------");
                Console.WriteLine("CR28-NESTED GROUPS");
                Console.WriteLine("------------------------------------");
                Console.WriteLine("(5) - Replace 'Active Directory Groups'.");
                Console.WriteLine();
                Console.WriteLine("------------------------------------");
                Console.WriteLine("BUGS");
                Console.WriteLine("------------------------------------");
                Console.WriteLine("(6) - [ESMA-1143] Restrict document(s) deletion permissions.");
                Console.WriteLine();


                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;
                option = Console.ReadLine();
                Console.WriteLine("");

                startedTime = DateTime.Now;

               if (option == "1")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("You selected: Paperless Routing Slip II usage (monthly) -> (Opt. 2, 3 and 4)");
                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("If you are sure press any key to continue...");
                    Console.ReadKey();

                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Starting the process...");

                    //--------------------------------------------------------------------
                    //
                    //--------------------------------------------------------------------
                    PaperlessUsage.GetTotalWFsPerWFTypeModule(); //WFs
                    PaperlessUsage.GetTotalDocumentsPerWFTypeModule(); //Attached Documents
                    PaperlessUsage.GetTotalLogsPerWFTypeModule();//Logs

                }
                else if (option == "2")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("You selected: Workflows created per workflow types.");
                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("If you are sure press any key to continue...");
                    Console.ReadKey();

                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Starting the process...");

                    //--------------------------------------------------------------------
                    //
                    //--------------------------------------------------------------------
                    PaperlessUsage.GetTotalWFsPerWFTypeModule();


                }
                else if (option == "3")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("You selected: Number of documents per workflow types.");
                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("If you are sure press any key to continue...");
                    Console.ReadKey();

                    Console.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Starting the process...");

                    //--------------------------------------------------------------------
                    //
                    //--------------------------------------------------------------------
                    PaperlessUsage.GetTotalDocumentsPerWFTypeModule();
                
                }
               else if (option == "4")
               {
                   Console.ForegroundColor = ConsoleColor.Green;
                   Console.WriteLine("You selected: Number of logs per workflow types.");
                   Console.WriteLine("");
                   Console.ForegroundColor = ConsoleColor.White;
                   Console.WriteLine("If you are sure press any key to continue...");
                   Console.ReadKey();

                   Console.WriteLine("");
                   Console.ForegroundColor = ConsoleColor.Green;
                   Console.WriteLine("Starting the process...");

                   //--------------------------------------------------------------------
                   //
                   //--------------------------------------------------------------------
                   PaperlessUsage.GetTotalLogsPerWFTypeModule();

               }
               else if (option == "5")
               {
                   Console.ForegroundColor = ConsoleColor.Green;
                   Console.WriteLine("You selected: Replace 'Active Directory Groups'");
                   Console.WriteLine("");
                   Console.ForegroundColor = ConsoleColor.White;
                   Console.WriteLine("If you are sure press any key to continue...");
                   Console.ReadKey();

                   Console.WriteLine("");
                   Console.ForegroundColor = ConsoleColor.Green;
                   Console.WriteLine("Starting the process...");

                   //--------------------------------------------------------------------
                   //
                   //--------------------------------------------------------------------
                   CR28_NestedGroups.ReplaceADGroupsModule();

               }
               else if (option == "6")
               {
                   Console.ForegroundColor = ConsoleColor.Green;
                   Console.WriteLine("You selected: [ESMA-1143] Restrict document(s) deletion permissions");
                   Console.WriteLine("");
                   Console.ForegroundColor = ConsoleColor.White;
                   Console.WriteLine("If you are sure press any key to continue...");
                   Console.ReadKey();

                   Console.WriteLine("");
                   Console.ForegroundColor = ConsoleColor.Green;
                   Console.WriteLine("Starting the process...");

                   //--------------------------------------------------------------------
                   //
                   //--------------------------------------------------------------------
                   Bug1143.FixPermissionsModule();
               }
                


            }
            catch (Exception ex)
            {
                iErrores++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("");
                Console.WriteLine("The application encountered an exception:");
                Console.WriteLine("¡ERROR! " + ex.Message);
                General.TraceException(ex);
                Console.ReadLine();


            }

            finally
            {
                TimeSpan dDuracion = DateTime.Now.Subtract(startedTime);
                //
                Console.WriteLine("");
                Console.WriteLine("==============================================================");
                Console.WriteLine("MODULE: " + Convert.ToString(option));
                Console.WriteLine("==============================================================");
                Console.WriteLine("PROCESS STARTED " + Convert.ToString(startedTime));
                Console.WriteLine("PROCESS FINISHED " + DateTime.Now.ToString());
                Console.WriteLine("- Process time: " + dDuracion.Duration().Hours + "h:" + dDuracion.Duration().Minutes + "m:" + dDuracion.Duration().Seconds + "s");



                if (iErrores > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("- Errors: " + iErrores.ToString());

                    Console.WriteLine("");
                    Console.WriteLine("PRESS ANY KEY TO CONTINUE...");

                    Console.ReadKey();

                }


                if (iErrores == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("");
                    Console.WriteLine("*** THE PROCESS HAS BEEN EXECUTED CORRECTLY. ***");


                    Console.WriteLine("");
                    Console.WriteLine("PRESS ANY KEY TO CONTINUE...");

                    Console.ReadKey();

                }


            }


            
        }


       
    }
}
