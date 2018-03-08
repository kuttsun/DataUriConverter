using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Extensions.Logging;

using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace HtmlBase64Converter
{
    class Converter
    {
        ILogger logger;
        Base64 base64;

        public Converter(ILogger<Converter> logger)
        {
            this.logger = logger;
            base64 = new Base64("UTF-8");
        }

        public void Start(string inputFile, string outputFile, bool replaceWithString = false)
        {
            var doc = default(IHtmlDocument);
            var parser = new HtmlParser();

            using (var fs = new FileStream(inputFile, FileMode.Open))
            {
                doc = parser.Parse(fs);
            }

            if (replaceWithString == false)
            {
                Replace(doc);
            }
            else
            {
                var html = ReplaceWithString(doc);
                doc = parser.Parse(html);
            }

            using (var fs = new FileStream(outputFile ?? inputFile, FileMode.Create))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(doc.DocumentElement.OuterHtml);
            }
        }

        void Replace(IHtmlDocument doc)
        {
            foreach (var item in doc.QuerySelectorAll("img"))
            {
                var src = item.Attributes["src"].Value;

                try
                {
                    var bytes = File.ReadAllBytes(src);
                    item.Attributes["src"].Value = $"{GetImageData(src)}{base64.Encode(bytes)}";
                    Console.WriteLine($"Complete: {src}");
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine($"Directory Not found: {src}");
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"File Not found: {src}");
                }
            }
        }

        string ReplaceWithString(IHtmlDocument doc)
        {
            //var pattern = @".*<img.*src\s*=\s*[\""|\'](.*?)[\""|\'].*>.*";
            //var pattern = ".*<img src=\\\\\"(.*?)\\\\\" .*>.*";
            var pattern = @"(.*<img src=\\"")(.*?)(\\"" .*>.*)";
            var rx = new Regex(pattern);
            return rx.Replace(doc.DocumentElement.OuterHtml, MatchEvaluatorMethod);
        }

        // 正規表現で一致した値を加工して置換するためのメソッド
        string MatchEvaluatorMethod(Match m)
        {
            var file = m.Groups[2].Value;
            if (file.StartsWith("file://"))
            {
               file= file.Remove(0,"file://".Length);
            }
            var bytes = File.ReadAllBytes(file);
            var ret = m.Groups[1].Value + GetImageData(file) + base64.Encode(bytes) + m.Groups[3].Value;
            Console.WriteLine($"Complete: {file}");
            return ret;
        }

        string GetImageData(string file)
        {
            switch (Path.GetExtension(file))
            {
                case ".png":
                    return "data:image/png;base64,";
                case ".jpg":
                    return "data:image/jpg;base64,";
            }

            Console.WriteLine($"Unknown image:{file}");
            return "";
        }
    }
}
