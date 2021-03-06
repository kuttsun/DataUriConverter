﻿using System;
using System.Text;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.CommandLineUtils;

namespace DataUriConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            // see http://kagasu.hatenablog.com/entry/2016/12/07/004813
            // required System.Text.Encoding.CodePages
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // required Microsoft.Extensions.DependencyInjection
            IServiceCollection serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            CommandLine(serviceProvider).Execute(args);

            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole() // required Microsoft.Extensions.Logging.Console
                .AddDebug();// required Microsoft.Extensions.Logging.Debug

            services.AddSingleton(loggerFactory);
            services.AddLogging();

            // IConfigurationBuilder で設定を選択
            // IConfigurationBuilder.Build() で設定情報を確定し、IConfigurationRoot を生成する
            IConfigurationRoot configuration = new ConfigurationBuilder()
                // 基準となるパスを設定
                // required Microsoft.Extensions.Configuration.FileExtensions
                .SetBasePath(Directory.GetCurrentDirectory())
                // ここでどの設定元を使うか指定
                // 同じキーが設定されている場合、後にAddしたものが優先される
                //.AddJsonFile("appsettings.json", optional: false)
                //.AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                // ここでは JSON より環境変数を優先している
                //.AddEnvironmentVariables()
                // 上記の設定を実際に適用して構成読み込み用のオブジェクトを得る
                .Build();

            // Logger と同じく DI サービスコンテナに Singleton ライフサイクルにてオブジェクトを登録する
            services.AddSingleton(configuration);

            // オプションパターンを有効にすることで、構成ファイルに記述した階層構造データを POCO オブジェクトに読み込めるようにする
            services.AddOptions();

            // Application を DI サービスコンテナに登録する
            // AddTransient はインジェクション毎にインスタンスが生成される
            services.AddTransient<Converter>();
        }

        static CommandLineApplication CommandLine(IServiceProvider serviceProvider)
        {
            // プログラム引数の解析
            var cla = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                // アプリケーション名（ヘルプの出力で使用される）
                Name = Assembly.GetExecutingAssembly().GetName().Name,
            };

            // Default behavior
            SetCommand(cla, serviceProvider, false);

            // backslide 専用
            cla.Command("backslide", command =>
            {
                cla.Description = "for backslide";
                SetCommand(command, serviceProvider, true);
            });

            return cla;
        }

        static void SetCommand(CommandLineApplication cla, IServiceProvider serviceProvider, bool backslide)
        {
            cla.HelpOption("-?|-h|--help");

            var input = cla.Option("-i|--input", "Input file", CommandOptionType.SingleValue);
            var output = cla.Option("-o|--output", "Output file", CommandOptionType.SingleValue);
            var dir = cla.Option("-d|--directory", "Input directory", CommandOptionType.SingleValue);

            cla.OnExecute(() =>
            {
                if (input.HasValue() && dir.HasValue())
                {
                    Console.WriteLine("-i と -d の両方を指定することはできません");
                    return 0;
                }

                var converter = serviceProvider.GetService<Converter>();
                if (dir.HasValue())
                {
                    converter.StartInDir(dir.Value(), backslide);
                }
                else
                {
                    converter.Start(input.Value(), output.Value(), backslide);
                }
                return 0;
            });
        }
    }
}
