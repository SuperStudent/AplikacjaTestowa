using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ftp {
    static class FtpError {
        public const string Connect = "podczas połączenia.";
        public const string Login = "podczas podawania loginu.";
        public const string Password = "podczas podawania hasła.";
        public const string Dir = "podczas zmiany katalogu docelowego.";
        public const string List = "podczas listowania plików.";
        public const string Pasive = "podczas przechodzenia w tryb pasywny.";
        public const string Retr = "podczas pobierania pliku.";
        public const string Type = "podczas zmiany typu transferu.";
        public const string Mtime = "podczas pobierania daty modyfikacji.";
        public const string Quit = "przy rozłączaniu.";
    }
}
