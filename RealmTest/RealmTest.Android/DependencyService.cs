using Android.App;
using Java.IO;
using Java.Lang;
using RealmTest.Droid;

[assembly: Xamarin.Forms.Dependency(typeof(RealmTestDependencyService))]
namespace RealmTest.Droid
{
    public class RealmTestDependencyService : IDependencyService
    {
        public long GetDbSize()
        {
            long size = -1;

            Process process = null;
            BufferedReader br = null;

            try
            {
                string line;
                const string command = "du -k realm.db";
                process = Runtime.GetRuntime().Exec(command, null, Application.Context.FilesDir);
                var stderr = process.ErrorStream;
                var stdout = process.InputStream;

                const string sep = "\t";
                using var isr = new InputStreamReader(stdout);
                br = new BufferedReader(isr);
                while ((line = br.ReadLine()) != null)
                {
                    var lineTrmimmed = line.Split(sep.ToCharArray())[0];
                    long.TryParse(lineTrmimmed, out size);
                }
                br.Close();
                br.TryDispose();

                using var isr2 = new InputStreamReader(stderr);
                br = new BufferedReader(isr2);
                while ((line = br.ReadLine()) != null)
                {
                    LogBroker.Instance.TraceError($"DB Size fetch Error: {line}", false);
                }
                br.Close();

                process.WaitFor();
            }
            catch (IOException ex)
            {
                Utils.TraceException(ex, false);
            }
            finally
            {
                br?.Close();
                br.TryDispose();
                process?.Destroy();
                process.TryDispose();
            }

            return size;
        }
    }
}