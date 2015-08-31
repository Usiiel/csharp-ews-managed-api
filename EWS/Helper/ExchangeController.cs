using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;//Agregada
using System.Text;
using System.Threading.Tasks;

namespace EWS.Helper
{
    class ExchangeController
    {
        private string username = "usuario@mail.com";
        private string password = "password";
        ExchangeService service;

        private bool Connect()
        {
            try
            {
                if (service == null)
                {
                    Console.WriteLine("Conectando...");
                    service = new ExchangeService(ExchangeVersion.Exchange2013_SP1); //creamos una nueva instancia de ExchangeService
                    service.Credentials = new NetworkCredential(username, password); //le entregamos las credenciales.
                    service.AutodiscoverUrl(username, RedirectionUrlValidationCallback); //mediante un callback obtenemos el endpoint especifico de la cuenta de correo.
                }
                Console.WriteLine("Conectado.");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool RedirectionUrlValidationCallback(String redirectionUrl)
        {
            bool redirectionValidated = false;
            if (redirectionUrl.Equals("https://autodiscover-s.outlook.com/autodiscover/autodiscover.xml"))
                redirectionValidated = true;

            return redirectionValidated;
        }

        private FolderId getFolderID(string FolderName)
        {
            Folder rootfolder = Folder.Bind(service, WellKnownFolderName.MsgFolderRoot); //Obtenemos las carpetas raiz de mensajes.
            FolderId folderID = null;

            FindFoldersResults folders = rootfolder.FindFolders(new SearchFilter.ContainsSubstring(FolderSchema.DisplayName, FolderName), new FolderView(10));
            if (folders.Count() > 0)
            {
                folderID = folders.ToList<Folder>()[0].Id;
            }

            return folderID;
        }

        private bool Procesar(Item item)
        {
            bool respuesta = false;
            try
            {
                item.Load();//cargamos el resto del mensaje.
                MessageBody messageBody = item.Body;//obtenemos el Body del mensaje

                //DeleteMode.HardDelete Eliminamos completamente el item.
                //DeleteMode.MoveToDeletedItems Movemos el item a la carpeta de eliminados
                //DeleteMode.SoftDelete Depende de ExchangeVersion, pero principalmente permite la recuperacion del item despues de eliminado.

                if (item.HasAttachments)//si tiene adjuntos 
                    ProcesarAdjuntos(item);
                else
                                if (item.Subject.ToLower().Contains("eliminar") || messageBody.Text.ToLower().Contains("eliminar")) // Revisamos si el asunto o en el body contiene la palabra "eliminar"
                {
                    item.Delete(DeleteMode.HardDelete);//Eliminamos completamente el item.
                    respuesta = true;
                    Console.WriteLine("Item Eliminado.");
                }
                else if (item.Subject.ToLower().Contains("mover") || messageBody.Text.ToLower().Contains("mover")) // Revisamos si el asunto o en el body contiene la palabra "mover"
                {
                    FolderId folderID = getFolderID("procesados");

                    if (folderID != null)//si encontramos la carpeta la 
                    {
                        item.Move(folderID);//movemos el item.
                        respuesta = true;
                        Console.WriteLine("Item Movido.");
                    }
                }
                else
                {
                    item.Delete(DeleteMode.MoveToDeletedItems);//lo movemos a la papelera
                    Console.WriteLine("Item Movido a carpeta Eliminados");
                }

            }
            catch (Exception)
            {

            }
            return respuesta;
        }

        private void ProcesarAdjuntos(Item item)
        {

            if (item.HasAttachments)//solo si tenemos adjuntos
                //obtenemos cada uno de los adjuntos contenidos en la collection Attachments
                foreach (Attachment adjunto in item.Attachments)
                {
                    if (adjunto is FileAttachment)// si es un archivo adjunto
                    {
                        FileAttachment archivoAdjunto = adjunto as FileAttachment;

                        // Load the attachment into a file.
                        // This call results in a GetAttachment call to EWS.
                        archivoAdjunto.Load("C:\\temp\\" + archivoAdjunto.Name);

                        Console.WriteLine("nombre del archivo adjunto: " + archivoAdjunto.Name);
                    }
                    else // si es un item adjunto
                    {
                        ItemAttachment itemAdjunto = adjunto as ItemAttachment;

                        //cargamos el adjunto en memoria.
                        //esta llamada resulta en una llamada GetAttachment a EWS
                        itemAdjunto.Load();

                        Console.WriteLine("nombre del item adjunto: " + itemAdjunto.Name);
                    }
                    FolderId folderID = getFolderID("procesados");

                    if (folderID != null)//si encontramos la carpeta la 
                    {
                        item.Move(folderID);//movemos el item.
                        Console.WriteLine("Item Movido.");
                    }
                }
        }


        public void Start()
        {
            if (!Connect())//si no se logra conectar entonces terminamos
            {
                Console.WriteLine("no fue posible conectarse.");
                return;
            }

            FindItemsResults<Item> findResults = service.FindItems(
               WellKnownFolderName.Inbox,
               new ItemView(10));

            Console.WriteLine("Correos en Inbox: " + findResults.Count());

            foreach (Item item in findResults.Items)
            {
                Procesar(item);//revision y procesamiento basico de un item.
            }
            Console.WriteLine("Terminamos.");
        }


    }
}
