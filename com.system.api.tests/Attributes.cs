using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace com.system.api.tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RepeatedTestMethodAttribute : TestMethodAttribute
    {
        protected int _count { get; }
        protected Formatting _formatting { get; }

        public RepeatedTestMethodAttribute(int count = -1) : this(count, Core.Formatting)
        {
        }

        public RepeatedTestMethodAttribute(int count, Formatting formatting)
        {
            // NOTE : DONT THROW EXCEPTIONS FROM HERE IT WILL CRASH GetCustomAttributes() call

            _formatting = formatting;
            _count = count > 0 ? count : Core.RepeatCount; 
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            TestResult testResult = testMethod.Invoke(null);

            var testContextMessages = JsonConvert.DeserializeObject<List<dynamic>>(testResult.TestContextMessages);

            var count = 1;
            while (count++ < _count)
            {
                testResult.InnerResultsCount = count;

                var iteration = testMethod.Invoke(null);

                var iterationContextMessages = JsonConvert.DeserializeObject<List<dynamic>>(iteration.TestContextMessages);
                testContextMessages.AddRange(iterationContextMessages);
            }

            testResult.TestContextMessages = JsonConvert.SerializeObject(testContextMessages, _formatting);

            return new TestResult[] { testResult };
        }
    }
}
