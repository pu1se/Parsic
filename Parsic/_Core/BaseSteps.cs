using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoTestic._Core
{
    public abstract class BaseSteps
    {
        protected WebPage Page { get; private set; }
        protected Settings Settings { get; }

        protected BaseSteps(WebPage page)
        {
            Page = page;
            Settings = new Settings();
        }

        protected string GetStepName(string param1 = "", string param2 = "", [CallerFilePath]string filePath = "", [CallerMemberName] string nameOfStep = "")
        {
            if (!filePath.EndsWith(".cs"))
            {
                return string.Empty;
            }

            var arr = filePath.Split("\\");
            var className = arr.Last().Replace(".cs", string.Empty);

            var methodInfo = GetTypeOfBaseStepsType(className)?.GetMethod(nameOfStep);
            if (methodInfo == null)
            {
                return string.Empty;
            }

            var attributes = methodInfo.GetCustomAttributes();
            var result = string.Empty;
            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case ThenAttribute thenAttribute:
                    {
                        var text = thenAttribute.Regex;
                        result = GetTextWithParams(param1, param2, text);
                        break;
                    }
                    case WhenAttribute whenAttribute:
                    {
                        var text = whenAttribute.Regex;
                        result = GetTextWithParams(param1, param2, text);
                        break;
                    }
                }
            }

            return result;
        }

        private static string GetTextWithParams(string param1, string param2, string text)
        {
            string[] arr;
            arr = text.Split("(.*)");
            var textWithParams = string.Empty;
            for (var i = 0; i < arr.Length; i++)
            {
                textWithParams += arr[i];
                if (i == 0 && !param1.IsNullOrEmpty())
                {
                    textWithParams += param1;
                }

                if (i == 1 && !param2.IsNullOrEmpty())
                {
                    textWithParams += param2;
                }
            }

            return textWithParams;
        }

        private Type GetTypeOfBaseStepsType(string className)
        {
            
            // Get all loaded assemblies
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var baseType = typeof(BaseSteps);
            // Search for types that inherit from the baseType
            var step = Assembly.GetCallingAssembly().GetTypes()
                .Where(type => baseType.IsAssignableFrom(type) && type != baseType)
                .FirstOrDefault(x => x.Name == className);
            return step!;
        }

        protected async Task CheckIsTrue(bool condition, string message)
        {
            if (condition == false)
            {
                var imagePath = await Page.MakeScreenShot();
                Assert.IsTrue(condition, message + $" Screenshot: {imagePath}");
            }
        }
    }
}
