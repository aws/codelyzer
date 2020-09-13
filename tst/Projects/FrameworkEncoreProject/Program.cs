using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;

namespace FrameworkEncoreProject
{
    class Program
    {
        static void Main(string[] args)
        {

            // Start of the method
            var workspace = MSBuildWorkspace //Namespace comment
                            .Create(); // Method comment


            // Local variables
            string name = "Name of the method";
            string p1 = "Some text";
            string p2 = "V";


            //This is sample comment
            Console.WriteLine(String.Format("Value returned from fw method is {0} ---> {1}",
                FrameworkClass
                    .setFrameworkProperty(
 /* one pre comment*/   p1, //parameter one comment
 /* two pre comment*/   p2  // parameter two comment
                    ), // Endof Method comment
                name)); //Comment at Top Method invocation

            string param1 = "FirstParameter";
            int param2 = 5;

            var temp1 = FrameworkClass.setFrameworkProperty(p1, p2); //same line

            var temp2 = FrameworkClass.setFrameworkProperty(p1, // just p1
                                            p2); // method p2
            /*
                        var temp2 =
                         FrameworkClass
                            .frameworkOnlyMethod(
                                param1, // comment for param1
                                param2  // comment for param2
                            ); */
        }
    }
}

