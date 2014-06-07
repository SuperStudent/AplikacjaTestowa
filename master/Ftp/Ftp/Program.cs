using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ftp {
    class Program {
        static void Main(string[] args) {
            Ftp A = new Ftp();

            try {
                if(args.Length == 2) {
                    A.Resolve(args[0], args[1]);
                }
                else {
                    A.Resolve(args[0], args[1], args[2]);
                }
                A.Run();
            }
            catch(IndexOutOfRangeException) {
                Console.WriteLine("Niepoprawna składnia. Spróbuj: Ftp <user>@<adres_ftp>/<katalog> <cel> [-level]");
            }
            catch(Exception ex) {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }

            Console.WriteLine("Gotowe.");
            Console.ReadKey();
        }
    }
}
