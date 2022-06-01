using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codelyzer.Analysis.Analyzer
{
    class CSharpAnalyzerFactory : LanguageAnalyzerFactory
    {
        protected readonly AnalyzerConfiguration _analyzerConfiguration;
        protected readonly ILogger _logger;

        public CSharpAnalyzerFactory(AnalyzerConfiguration analyzerConfiguration, ILogger logger)
        {
            _analyzerConfiguration = analyzerConfiguration;
            _logger = logger;
        }
        public override LanguageAnalyzer GetLanguageAnalyzer()
        {
            return new CSharpAnalyzer(_analyzerConfiguration, _logger);
        }
    }

    /*class MoneyBackFactory : CardFactory
    {
        private int _creditLimit;
        private int _annualCharge;

        public MoneyBackFactory(int creditLimit, int annualCharge)
        {
            _creditLimit = creditLimit;
            _annualCharge = annualCharge;
        }

        public override CreditCard GetCreditCard()
        {
            return new MoneyBackCreditCard(_creditLimit, _annualCharge);
        }
    }*/
}
