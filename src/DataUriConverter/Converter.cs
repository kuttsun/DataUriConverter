using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Microsoft.Extensions.Logging;

using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace DataUriConverter
{
    class Converter
    {
        ILogger logger;
        Base64 base64;
        HtmlParser parser;

        public Converter(ILogger<Converter> logger)
        {
            this.logger = logger;
            base64 = new Base64("UTF-8");
            parser = new HtmlParser();
        }

        public void StartInDir(string dir, bool replaceWithString)
        {
            var files = Directory.GetFiles(dir, "*.html", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                Start(file, null, replaceWithString);
            }
        }

        public void Start(string inputFile, string outputFile, bool replaceWithString)
        {
            logger.LogInformation($"[Input] {inputFile}");

            var doc = default(IHtmlDocument);

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
                    var data = GetDataUri(src);
                    if (data != null)
                    {
                        item.Attributes["src"].Value = data;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    logger.LogError($"Directory Not found: {src}");
                }
                catch (FileNotFoundException)
                {
                    logger.LogError($"File Not found: {src}");
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
            string ret = file;

            var data = GetDataUri(file);
            if (data != null)
            {
                ret = data;
            }

            return m.Groups[1].Value + ret + m.Groups[3].Value;
        }

        string GetDataUri(string file)
        {
            // 既に Data URI になっている場合は何もしない
            if (file.StartsWith("data:image"))
            {
                logger.LogInformation($"[Skip]");
                return null;
            }

            if (file.StartsWith("file://"))
            {
                file = file.Remove(0, "file://".Length);
            }

            string header = "";
            string content = "";

            switch (Path.GetExtension(file))
            {
                case ".png":
                    header = "data:image/png;base64,";
                    content = base64.Encode(File.ReadAllBytes(file));
                    break;
                case ".jpg":
                    header = "data:image/jpg;base64,";
                    content = base64.Encode(File.ReadAllBytes(file));
                    break;
                case ".svg":
                    header = "data:image/svg+xml;base64,";
                    content = ConvertSvg(file);
                    break;
                default:
                    logger.LogWarning($"Unknown image:{file}");
                    return null;
            }

            logger.LogInformation($"[Converterd] {file}");

            return header + content;
        }

        string ConvertSvg(string file)
        {
            var doc = default(IHtmlDocument);
            string ret = null;

            using (var fs = new FileStream(file, FileMode.Open))
            {
                doc = parser.Parse(fs);
            }

            ret = doc.QuerySelector("svg").OuterHtml;

            return ret;
        }
    }
}
