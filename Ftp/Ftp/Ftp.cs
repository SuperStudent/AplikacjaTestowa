using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace Ftp {
    public enum FtpCommand {
        User = 1,
        Pass = 2,
        Pasv = 3,
        Cwd = 4,
        Typei = 5,
        Retr = 6,
        List = 7,
        Mdtm = 8,
        Quit = 9
    }

    class Ftp {
        string adres;
        string konto;
        string hasło;
        string katalog;
        string cel;
        Socket polecenia;
        Socket dane;
        byte[] bufor;
        int maxDir;

        public Ftp() {
            this.polecenia = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.dane = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.bufor = null;
        }

        public void Resolve(string dane, string cel, string max = ""){
            this.cel = cel;

            if(max.Length > 0) {
                int temp;
                max = max.Substring(1);
                if(Int32.TryParse(max, out temp)) {
                    this.maxDir = temp;
                }
                else {
                    this.maxDir = Int32.MaxValue;
                }
            }
            else {
                this.maxDir = Int32.MaxValue;
            }        

            if(!Directory.Exists(this.cel)) {
                Directory.CreateDirectory(this.cel);
            }

            string[] dziel = dane.Split(new char[] { '/' }, 2);
            string[] dzieli = dziel[0].Split(new char[] { '@' }, 2);
            
            this.adres = dzieli[1];
            this.konto = dzieli[0];
            this.katalog = dziel[1];

            if(this.adres.Length == 0 || this.konto.Length == 0 || this.katalog.Length == 0) {
                throw new Exception("Niepoprawna składnia pierwszego wyrażenia. Przykład: moje_konto@ftp.qq.pl//user/katalog");
            }
        }

        public void Run() {
            this.polecenia.Connect(this.adres, 21);

            if(this.polecenia.Poll(0, SelectMode.SelectRead) && this.polecenia.Available == 0) {
                this.polecenia.Close();
                throw new Exception("Nie nawiązano połączenia.");
            }

            string wynik = "";

            wynik = this.getAnswer(FtpError.Connect);

            this.execCommand(FtpCommand.User, this.konto);
            wynik = this.getAnswer(FtpError.Login);

            this.getPassword();

            this.execCommand(FtpCommand.Pass, this.hasło);
            wynik = this.getAnswer(FtpError.Password);

            this.execCommand(FtpCommand.Cwd, this.katalog);
            wynik = this.getAnswer(FtpError.Dir);

            this.execCommand(FtpCommand.Pasv);
            wynik = this.getAnswer(FtpError.Pasive);

            this.Connect(wynik);

            this.execCommand(FtpCommand.List);
            wynik = this.getAnswer(FtpError.List);
            wynik = getData(FtpError.List);

            Katalog pliki = new Katalog(this.katalog);
            pliki.setMax(this.maxDir);
            pliki.Reslove(wynik);

            wynik = this.getAnswer(FtpError.List);

            while(true) {
                wynik = pliki.nextDir();
                if(wynik == this.katalog) {
                    break;
                }

                this.execCommand(FtpCommand.Cwd, wynik);
                wynik = this.getAnswer(FtpError.Dir);

                this.execCommand(FtpCommand.Pasv);
                wynik = this.getAnswer(FtpError.Pasive);

                this.Connect(wynik);

                this.execCommand(FtpCommand.List);
                wynik = this.getAnswer(FtpError.List);
                wynik = getData(FtpError.List);

                pliki.Reslove(wynik);

                wynik = this.getAnswer(FtpError.List);
            }

            this.execCommand(FtpCommand.Typei);
            wynik = this.getAnswer(FtpError.Type);

            bool first = false;
            
            while(true) {
                string current = this.katalog;
               
                if(first) {
                    current = pliki.nextDir();

                    if(current == this.katalog) {
                        break;
                    }
                }

                string folder = this.cel + pliki.getShortName();
                
                if(!Directory.Exists(folder)) {
                    Directory.CreateDirectory(folder);
                }

                this.execCommand(FtpCommand.Cwd, current);
                wynik = this.getAnswer(FtpError.Dir);
                string[] wyniki = pliki.getFiles();

                foreach(string Q in wyniki) {
                    DateTime czas = DateTime.Now;

                    if(File.Exists(folder + "/" + Q)) {
                        DateTime mtime = File.GetLastWriteTime(folder + "/" + Q);

                        this.execCommand(FtpCommand.Mdtm, Q);
                        wynik = this.getAnswer(FtpError.Mtime);

                        czas = this.getDate(wynik);

                        if(mtime >= czas) {
                            continue;
                        }
                    }
                    else {
                        this.execCommand(FtpCommand.Mdtm, Q);
                        wynik = this.getAnswer(FtpError.Mtime);

                        czas = this.getDate(wynik);
                    }

                    this.execCommand(FtpCommand.Pasv);
                    wynik = this.getAnswer(FtpError.Pasive);

                    this.Connect(wynik);
                    
                    this.execCommand(FtpCommand.Retr, Q);
                    wynik = this.getAnswer(FtpError.List);
                    this.getFile(folder, Q, czas);
                    wynik = this.getAnswer(FtpError.Retr);
                    Console.WriteLine("Pobrano plik: " + Q);
                }
                first = true;
            }

            this.execCommand(FtpCommand.Quit);
            this.getAnswer(FtpError.Quit);        
        }

        void execCommand(FtpCommand cmd, string arg = null) {
            string message = "";

            switch(cmd) {
                case FtpCommand.User:
                    if(arg == null || arg.Length == 0) {
                        throw new Exception("Brak nazwy usera w komendzie USER.");
                    }
                    message += "USER " + arg + "\r\n";
                    break;
                case FtpCommand.Pass:
                    if(arg == null || arg.Length == 0) {
                        throw new Exception("Brak hasła w komendzie PASS.");
                    }
                    message += "PASS " + arg + "\r\n";
                    break;
                case FtpCommand.Cwd:
                    if(arg == null || arg.Length == 0) {
                        throw new Exception("Brak katalogu w komendzie CWD.");
                    }
                    message += "CWD " + arg + "\r\n";
                    break;
                case FtpCommand.Retr:
                    if(arg == null || arg.Length == 0) {
                        throw new Exception("Brak pliku w komendzie RETR.");
                    }
                    message += "RETR " + arg + "\r\n";
                    break;
                case FtpCommand.Mdtm:
                    if(arg == null || arg.Length == 0) {
                        throw new Exception("Brak pliku w komendzie MDTM.");
                    }
                    message += "MDTM " + arg + "\r\n";
                    break;
                case FtpCommand.Pasv:
                    message += "PASV\r\n";
                    break;
                case FtpCommand.Typei:
                    message += "TYPE I\r\n";
                    break;
                case FtpCommand.List:
                    message += "LIST\r\n";
                    break;
                case FtpCommand.Quit:
                    message += "QUIT\r\n";
                    break;
            }

            this.bufor = System.Text.Encoding.GetEncoding("ascii").GetBytes(message);
            this.polecenia.Send(bufor);
        }

        string getAnswer(string cmd) {
            StringBuilder message = new StringBuilder();

            TimeSpan maxDuration = TimeSpan.FromSeconds(5);
            Stopwatch sw = Stopwatch.StartNew();
            while(sw.Elapsed < maxDuration && this.polecenia.Available <= 0) { }

            if(sw.Elapsed >= maxDuration && this.polecenia.Available <= 0) {
                throw new Exception("Przekroczono limit czasu oczekiwania.");
            }

            while(this.polecenia.Available > 0) {
                int bytes = this.polecenia.Available;
                this.bufor = new byte[bytes];

                this.polecenia.Receive(bufor);
                string part = this.isValid(bufor, cmd);
                message.Append(part);
            }

            return message.ToString();
        }

        string isValid(byte[] text, string state) {
            if(text == null) {
                throw new Exception("Otrzymano pustą wiadomość " + state);
            }

            string temp = System.Text.Encoding.UTF8.GetString(text);

            this.check(temp.Substring(0, 3), state);

            return temp;
        }

        void check(string num, string state) {
            short number;

            if(Int16.TryParse(num, out number)) {
                if(number > 399) {
                    throw new Exception("Odpowiedź serwera nie była pozytywna " + state + " Error: " + number.ToString());
                }
            }
        }

        void getPassword() {
            Console.Write("Podaj hasło: ");
            ConsoleKeyInfo keyinfo = new ConsoleKeyInfo();

            while(!(keyinfo = Console.ReadKey(true)).Key.Equals(ConsoleKey.Enter)) {
                if(keyinfo.Key.Equals(ConsoleKey.Backspace) && this.hasło.Length > 0) {
                    this.hasło = this.hasło.Remove(this.hasło.Length - 1);
                    Console.Write('\b');
                    Console.Write(' ');
                    Console.Write('\b');
                }
                else {
                    this.hasło += keyinfo.KeyChar;
                    Console.Write('*');
                }
            }

            if(this.hasło == "" || this.hasło.Contains(' ')) {
                throw new Exception("Hasło jest nieprawidłowe.");
            }

            Console.WriteLine();
        }

        string getData(string cmd) {
            StringBuilder message = new StringBuilder();

            TimeSpan maxDuration = TimeSpan.FromSeconds(5);
            Stopwatch sw = Stopwatch.StartNew();
            while(sw.Elapsed < maxDuration && this.dane.Available <= 0) { }

            if(sw.Elapsed >= maxDuration && this.dane.Available <= 0) {
                throw new Exception("Przekroczono limit czasu oczekiwania.");
            }

            while(this.dane.Available > 0) {
                int bytes = this.dane.Available;
                this.bufor = new byte[bytes];

                this.dane.Receive(bufor);
                string part = this.isValid(bufor, cmd);
                message.Append(part);

                maxDuration = TimeSpan.FromSeconds(5);
                sw = Stopwatch.StartNew();
                while(sw.Elapsed < maxDuration && this.dane.Available <= 0 && this.polecenia.Available <= 0) { }
            }

            return message.ToString();
        }

        void getFile(string path, string file, DateTime mtime) {
            FileStream plik = new FileStream(path + "/" + file, FileMode.Append);

            TimeSpan maxDuration = TimeSpan.FromSeconds(5);
            Stopwatch sw = Stopwatch.StartNew();
            while(sw.Elapsed < maxDuration && this.dane.Available <= 0) { }

            if(sw.Elapsed >= maxDuration && this.dane.Available <= 0) {
                throw new Exception("Przekroczono limit czasu oczekiwania.");
            }

            while(this.dane.Available > 0) {
                int bytes = this.dane.Available;
                this.bufor = new byte[bytes];

                this.dane.Receive(bufor);
                plik.Write(this.bufor, 0, bytes);

                maxDuration = TimeSpan.FromSeconds(5);
                sw = Stopwatch.StartNew();
                while(sw.Elapsed < maxDuration && this.dane.Available <= 0 && this.polecenia.Available <= 0) { }
            }

            plik.Close();
            File.SetLastWriteTime(path + "/" + file, mtime);
        }

        void Connect(string dane) {
            string[] wynik = dane.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            wynik = wynik[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            int a;
            int b;
            Int32.TryParse(wynik[4], out a);
            Int32.TryParse(wynik[5], out b);

            int port = 256 * a + b;

            string adresik = wynik[0] + "." + wynik[1] + "." + wynik[2] + "." + wynik[3];

            this.dane.Close();
            this.dane = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.dane.Connect(adresik, port);

            if(this.dane.Poll(0, SelectMode.SelectRead) && this.dane.Available == 0) {
                this.dane.Close();
                throw new Exception("Nie nawiązano połączenia danych.");
            }
        }

        DateTime getDate(string dane) {
            int year;
            int month;
            int day;
            int hour;
            int minute;
            int second;

            if(dane.Substring(0, 4) != "213 ") {
                Console.WriteLine("Ostrzeżenie: Nie udało sie otrzymać poprawnej daty modyfikacji. Ustawiana jest aktualna.");
                return DateTime.Now;
            }

            if(Int32.TryParse(dane.Substring(4, 4), out year) &&
            Int32.TryParse(dane.Substring(8, 2), out month) &&
            Int32.TryParse(dane.Substring(10, 2), out day) &&
            Int32.TryParse(dane.Substring(12, 2), out hour) &&
            Int32.TryParse(dane.Substring(14, 2), out minute) &&
            Int32.TryParse(dane.Substring(16, 2), out second)) {
                return new DateTime(year, month, day, hour, minute, second);
            }
            else {
                Console.WriteLine("Ostrzeżenie: Nie udało sie otrzymać poprawnej daty modyfikacji. Ustawiana jest aktualna.");
                return DateTime.Now;
            }

        }
    }
}
