using System.CommandLine;
using System.IO;

namespace Recrd.Cli.Tests;

public class TestConfig
{
    public void Test()
    {
        var command = new Command("test");
        var output = new StringWriter();
        // Try various configuration classes
        // var config = new CliConfiguration(command) { Output = output };
        // var config2 = new CommandLineConfiguration(command) { Output = output };
        // var config3 = new InvocationConfiguration { Output = output };
    }
}
