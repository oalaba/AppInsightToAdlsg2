using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AppIntToAdlsg2
{
    public class Program
    {
        const string CLIENTSECRET = "14e57ff6-b0e1-4249-a43a-13f58f925552";
        const string CLIENTID = "53a7858f-d594-41ee-bf53-7a634842fa24";
        const string BASESECRETURI = "https://npe-kv.vault.azure.net/"; // available from the Key Vault resource page

        static KeyVaultClient kvc = null;

        public static void Main(string[] args)
        {
            
            //UploadAppInsightsDataToADLSG2();

            //GetKeyVaultSecrets();
        }

        static void UploadAppInsightsDataToADLSG2(){

            string accountName = "dara0dev0adlsg2";

            string accountKey = "q5Hf/ROpDKS5jc3CGLGkWAgSRlkkz/2x0H+zQweY7iHp9An9iSgB3HaATh/argVX/jG4ZoFXg2H706/5MH40NQ==";

            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=" + accountName + ";AccountKey=" + accountKey;
            CloudStorageAccount account = CloudStorageAccount.Parse(storageConnectionString);

            ExecuteChoice(account);
        }
        public static void ExecuteChoice(CloudStorageAccount account)
        {
            int choice = 1;

            // SetNumberOfParallelOperations();

            if(choice == 1)
            {
                TransferLocalFileToAzureBlob(account).Wait();
            }
            else if(choice == 2)
            {
                TransferLocalDirectoryToAzureBlobDirectory(account).Wait();
            }
            else if(choice == 3)
            {
                TransferUrlToAzureBlob(account).Wait();
            }
            else if(choice == 4)
            {
                TransferAzureBlobToAzureBlob(account).Wait();
            }
        }

        public static async Task TransferLocalFileToAzureBlob(CloudStorageAccount account)
        {
            string localFilePath = GetSourcePath();
            CloudBlockBlob blob = GetBlob(account);
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = GetSingleTransferContext(checkpoint);
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            Console.WriteLine("\nTransfer started...\nPress 'c' to temporarily cancel your transfer...\n");

            Stopwatch stopWatch = Stopwatch.StartNew();
            Task task;
            ConsoleKeyInfo keyinfo;
            try
            {
                task = TransferManager.UploadAsync(localFilePath, blob, null, context, cancellationSource.Token);
                while(!task.IsCompleted)
                {
                    if(Console.KeyAvailable)
                    {
                        keyinfo = Console.ReadKey(true);
                        if(keyinfo.Key == ConsoleKey.C)
                        {
                            cancellationSource.Cancel();
                        }
                    }
                }
                await task;
            }
            catch(Exception e)
            {
                Console.WriteLine("\nThe transfer is canceled: {0}", e.Message);
            }

            if(cancellationSource.IsCancellationRequested)
            {
                Console.WriteLine("\nTransfer will resume in 3 seconds...");
                Thread.Sleep(3000);
                checkpoint = context.LastCheckpoint;
                context = GetSingleTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                await TransferManager.UploadAsync(localFilePath, blob, null, context);
            }

            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            //ExecuteChoice(account);
        }

        public static async Task TransferLocalDirectoryToAzureBlobDirectory(CloudStorageAccount account)
        {
            string localDirectoryPath = GetDirSourcePath();
            CloudBlobDirectory blobDirectory = GetBlobDirectory(account);
            TransferCheckpoint checkpoint = null;
            DirectoryTransferContext context = GetDirectoryTransferContext(checkpoint);
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            Console.WriteLine("\nTransfer started...\nPress 'c' to temporarily cancel your transfer...\n");

            Stopwatch stopWatch = Stopwatch.StartNew();
            Task task;
            ConsoleKeyInfo keyinfo;
            UploadDirectoryOptions options = new UploadDirectoryOptions()
            {
                Recursive = true
            };

            try
            {
                task = TransferManager.UploadDirectoryAsync(localDirectoryPath, blobDirectory, options, context, cancellationSource.Token);
                while(!task.IsCompleted)
                {
                    if(Console.KeyAvailable)
                    {
                        keyinfo = Console.ReadKey(true);
                        if(keyinfo.Key == ConsoleKey.C)
                        {
                            cancellationSource.Cancel();
                        }
                    }
                }
                await task;
            }
            catch(Exception e)
            {
                Console.WriteLine("\nThe transfer is canceled: {0}", e.Message);
            }

            if(cancellationSource.IsCancellationRequested)
            {
                Console.WriteLine("\nTransfer will resume in 3 seconds...");
                Thread.Sleep(3000);
                checkpoint = context.LastCheckpoint;
                context = GetDirectoryTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                await TransferManager.UploadDirectoryAsync(localDirectoryPath, blobDirectory, options, context);
            }

            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            //ExecuteChoice(account);
        }

        public static async Task TransferUrlToAzureBlob(CloudStorageAccount account)
        {
            Uri uri = new Uri(GetSourcePath());
            CloudBlockBlob blob = GetBlob(account);
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = GetSingleTransferContext(checkpoint);
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            Console.WriteLine("\nTransfer started...\nPress 'c' to temporarily cancel your transfer...\n");

            Stopwatch stopWatch = Stopwatch.StartNew();
            Task task;
            ConsoleKeyInfo keyinfo;
            try
            {
                task = TransferManager.CopyAsync(uri, blob, true, null, context, cancellationSource.Token);
                while(!task.IsCompleted)
                {
                    if(Console.KeyAvailable)
                    {
                        keyinfo = Console.ReadKey(true);
                        if(keyinfo.Key == ConsoleKey.C)
                        {
                            cancellationSource.Cancel();
                        }
                    }
                }
                await task;
            }
            catch(Exception e)
            {
                Console.WriteLine("\nThe transfer is canceled: {0}", e.Message);
            }

            if(cancellationSource.IsCancellationRequested)
            {
                Console.WriteLine("\nTransfer will resume in 3 seconds...");
                Thread.Sleep(3000);
                checkpoint = context.LastCheckpoint;
                context = GetSingleTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                await TransferManager.CopyAsync(uri, blob, true, null, context, cancellationSource.Token);
            }

            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            //ExecuteChoice(account);
        }

        public static async Task TransferAzureBlobToAzureBlob(CloudStorageAccount account)
        {
            CloudBlockBlob sourceBlob = GetBlob(account);
            CloudBlockBlob destinationBlob = GetBlob(account);
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = GetSingleTransferContext(checkpoint);
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            Console.WriteLine("\nTransfer started...\nPress 'c' to temporarily cancel your transfer...\n");

            Stopwatch stopWatch = Stopwatch.StartNew();
            Task task;
            ConsoleKeyInfo keyinfo;
            try
            {
                task = TransferManager.CopyAsync(sourceBlob, destinationBlob, true, null, context, cancellationSource.Token);
                while(!task.IsCompleted)
                {
                    if(Console.KeyAvailable)
                    {
                        keyinfo = Console.ReadKey(true);
                        if(keyinfo.Key == ConsoleKey.C)
                        {
                            cancellationSource.Cancel();
                        }
                    }
                }
                await task;
            }
            catch(Exception e)
            {
                Console.WriteLine("\nThe transfer is canceled: {0}", e.Message);
            }

            if(cancellationSource.IsCancellationRequested)
            {
                Console.WriteLine("\nTransfer will resume in 3 seconds...");
                Thread.Sleep(3000);
                checkpoint = context.LastCheckpoint;
                context = GetSingleTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                await TransferManager.CopyAsync(sourceBlob, destinationBlob, false, null, context, cancellationSource.Token);
            }

            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            //ExecuteChoice(account);
        }

        public static string GetSourcePath()
        {
            //Console.WriteLine("asset/source.txt");
            string sourcePath = "/Users/oluwadaraalaba/Documents/EngineeringData/Border_Crossing_Entry_Data.csv";

            return sourcePath;
        }

        public static string GetDirSourcePath()
        {
            //Console.WriteLine("asset/source.txt");
            string sourcePath = "/Users/oluwadaraalaba/Documents/EngineeringData/*";

            return sourcePath;
        }

        public static CloudBlockBlob GetBlob(CloudStorageAccount account)
        {
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            //Console.WriteLine("abike");
            string containerName = "data1";
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();

            //Console.WriteLine("document.txt");
            string blobName = "border_data.csv";
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            return blob;
        }

        public static void SetNumberOfParallelOperations()
        {
            //Console.WriteLine("2");
            string parallelOperations = "2";
            TransferManager.Configurations.ParallelOperations = int.Parse(parallelOperations);
        }

        public static SingleTransferContext GetSingleTransferContext(TransferCheckpoint checkpoint)
        {
            SingleTransferContext context = new SingleTransferContext(checkpoint);

            context.ProgressHandler = new Progress<TransferStatus>((progress) =>
            {
                Console.Write("\rBytes transferred: {0}", progress.BytesTransferred );
            });

            return context;
        }

        public static DirectoryTransferContext GetDirectoryTransferContext(TransferCheckpoint checkpoint)
        {
            DirectoryTransferContext context = new DirectoryTransferContext(checkpoint);

            context.ProgressHandler = new Progress<TransferStatus>((progress) =>
            {
                Console.Write("\rBytes transferred: {0}", progress.BytesTransferred );
            });

            return context;
        }

        public static CloudBlobDirectory GetBlobDirectory(CloudStorageAccount account)
        {
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            //Console.WriteLine("myblob");
            string containerName = "myblob";
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();

            CloudBlobDirectory blobDirectory = container.GetDirectoryReference("");

            return blobDirectory;
        }
    }
}