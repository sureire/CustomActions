using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace CustomAction1
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult RegisterServerCert(Session session)
        {
            session.Log("Begin CustomAction1");

            string path = session["SERVERCERT_PATH"];
            string port = session["EDIT_PORT"];
            string pwd = session["SERVER_PASSWORD"];

            //System.Windows.Forms.MessageBox.Show(pwd);


            //X509Certificate2 cert = InstallCertificate(path, pwd, StoreName.Root);
            // cert = InstallCertificate(path, pwd, StoreName.CertificateAuthority);
            // cert = InstallCertificate(path, pwd, StoreName.My);

            X509Certificate2 cert = new X509Certificate2(path, pwd);
            bool ret = BindSSL(session, port, cert.Thumbprint);
            if (!ret)
            {
                System.Windows.Forms.MessageBox.Show("Failed in Binding the port", "Error");
                return ActionResult.Failure;
            }
            session.Log("the path is " + path);

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult RegisterClientCert(Session session)
        {

            string path = session["CLIENTCERT_PATH"];
            string pwd = session["CLIENT_PASSWORD"];
            X509Certificate2 cert = InstallCertificate(path, pwd,StoreName.CertificateAuthority);
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult UpdatePort(Session session)
        {

            string port = session["EDIT_PORT"];
            int nport;
            if (int.TryParse(port, out nport))
            {
                var lines = File.ReadLines(session["APPDIR"] + "AppViewXServiceHost.exe.config");
                List<string> reslines = new List<string>();
                foreach (string line in lines)
                {
                    string st = line;
                    if (line.Contains("https://localhost:port"))
                        st = line.Replace("https://localhost:port", "https://localhost:" + nport);

                    if (line.Contains("http://localhost:port"))
                        st = line.Replace("http://localhost:port", "http://localhost:" + (nport + 1));

                    reslines.Add(st);
                }

                File.WriteAllLines(session["APPDIR"] + "AppViewXServiceHost.exe.config", reslines.ToArray());

            }

            //System.Windows.Forms.MessageBox.Show(servercertpath);
            //System.Windows.Forms.MessageBox.Show(port);
            //XElement xe = null;

            //xe = XElement.Load(session["APPDIR"] + "AppViewXServiceHost.exe.config", LoadOptions.None);

            //var attribute = xe.Descendants("baseAddresses").Elements().Attributes("baseAddress");
            //string st = attribute.SingleOrDefault().Value;
            //st = st.Replace("port", port);
            //attribute.SingleOrDefault().Value = st;
            ////System.Windows.Forms.MessageBox.Show(xe.ToString());
            //xe.Save(session["APPDIR"] + "AppViewXServiceHost.exe.config");
            return ActionResult.Success;
        }

        private static X509Certificate2 InstallCertificate(string cerFileName, string pwd, StoreName sn )
        {
            try
            {
                X509Certificate2 certificate = new X509Certificate2(cerFileName,pwd);
                X509Store store = new X509Store(sn, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();
                //System.Windows.Forms.MessageBox.Show("Certifcate added..", "Success");
                return certificate;

            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Error installing Certificate");
            }
            return null;
        }

        private static bool BindSSL(Session session, string port, string thumbprint)
        {
            string appid = Guid.NewGuid().ToString();


            string arguments = "http add sslcert ipport=0.0.0.0:" + port + " certhash=" + thumbprint + " certstorename=MY appid={" + appid + "} clientcertnegotiation=enable";
            ProcessStartInfo procStartInfo = new ProcessStartInfo("netsh", arguments);

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            procStartInfo.Verb = "runas";

            Process p1 = Process.Start(procStartInfo);

            string ret = p1.StandardOutput.ReadToEnd();

            session.Log(ret);

            if (ret.Contains("success"))
                return true;
            else
                return false;
        }
    }
}
