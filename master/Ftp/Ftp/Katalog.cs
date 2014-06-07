using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ftp {
    class Katalog {
        public List<Katalog> katalogi;
        public List<string> pliki;
        public int seek;
        int level;
        Katalog ptr;
        public Katalog root;
        string name;
        int max;
        public string shortName;

        public Katalog(string name) {
            this.katalogi = new List<Katalog>();
            this.pliki = new List<string>();
            this.seek = -1;
            this.level = 0;
            this.ptr = this;
            this.root = null;
            this.name = name;
            this.max = Int32.MaxValue;

            string[] wynik = name.Split(new char[]{'/'}, StringSplitOptions.RemoveEmptyEntries);

            if(wynik.Length > 0) {
                this.shortName = "/" + wynik[wynik.Length - 1];
            }
            else {
                this.shortName = "/";
            }
        }

        public void Reslove(string dane) {
            string[] wynik = dane.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach(string Q in wynik) {
                string name = this.getFileName(Q);
                
                if(Q[0] == 'd') {
                    if(this.level < this.max) {
                        Katalog temp = new Katalog(this.ptr.getName() + "/" + name);
                        temp.root = this.ptr;
                        temp.shortName = this.ptr.shortName + "/" + name;
                        this.ptr.katalogi.Add(temp);
                    }
                }
                else {
                    this.ptr.pliki.Add(name);
                }
            }
        }

        string getFileName(string dane) {
            string[] wynik = dane.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return wynik[8];
        }

        public string[] getFiles() {
            return this.ptr.pliki.ToArray();
        }

        public void resetSeek(){
            this.ptr.seek = -1;
        }

        public int count() {
            return this.ptr.pliki.Count;
        }

        public int dirCount() {
            return this.katalogi.Count;
        }

        public void setMax(int number) {
            this.max = number;
        }

        public string getName(){
            return this.name;
        }

        public string nextDir() {
            this.ptr.seek += 1;

            if(this.ptr.seek >= this.ptr.dirCount()){
                if(this.ptr == this){
                    this.seek = -1;
                    return this.name;
                }
                else{
                    this.ptr.seek = -1;
                    this.ptr = this.ptr.root;
                    this.level--;
                    return this.nextDir();
                }
            }
            
            this.ptr = this.ptr.katalogi[this.ptr.seek];
            this.level++;

            return this.ptr.getName();
        }

        public string getShortName() {
            return this.ptr.shortName;
        }

    }
}
