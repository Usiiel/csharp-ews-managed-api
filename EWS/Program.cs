using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EWS
{
    class Program
    {
        static void Main(string[] args)
        {
            Helper.ExchangeController exchangeController = new Helper.ExchangeController();
            exchangeController.Start();
            Console.ReadLine();
        }
    }
}
