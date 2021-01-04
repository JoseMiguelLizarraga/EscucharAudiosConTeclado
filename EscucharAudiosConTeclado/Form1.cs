using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Recognition;
using System.Media;   // Reproducir sonido
using NAudio.Wave;
using System.Speech.Synthesis;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using EscucharAudiosConTeclado.Clases;

namespace EscucharAudiosConTeclado
{
    public partial class Form1 : Form
    {

        private Keys teclaPlay = (Keys) new KeysConverter().ConvertFromString(ConfigurationManager.AppSettings["teclaPlay"]);
        private Keys teclaStop = (Keys) new KeysConverter().ConvertFromString(ConfigurationManager.AppSettings["teclaStop"]);

        private string numeroDiscoSeleccionado = "";
        private string rutaArchivosAudio = ConfigurationManager.AppSettings["rutaArchivosAudio"];  // C:/Users/josel/Downloads/Sonidos

        private Boolean ha_confirmado_el_usuario_que_escucha = false;

        private Boolean iniciar_proceso_apagar_pc = false;
        private Boolean esta_el_sistema_hablando = true;
        private Boolean esta_la_pantalla_seleccionada = true;
        private Boolean ha_pulsado_tecla = false;             // teclaPulsada
        private Boolean existeDiscoSeleccionado = false;
        private Boolean sonidoPausado = false;
        private IWavePlayer salidaSonido = null;
        private SpeechSynthesizer vozInstrucciones = new SpeechSynthesizer();

        private List<ArchivoAudio> listaArchivoAudio = new List<ArchivoAudio>();
        private ArchivoAudio discoSeleccionado = null;

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;        // Coloca la aplicacion en pantalla completa
            this.BackColor = System.Drawing.Color.GreenYellow;   // El color de fondo sera verde amarillento
            this.confirmarUsuarioEscuchando();                   // Lanza un mensaje para ver si el usuario esta escuchando
            timer1.Start();                                      // Inicia el timer
        }

        private void iniciarCargaArchivosDeAudio()
        {
            DirectoryInfo directorio = new DirectoryInfo(rutaArchivosAudio);
            FileInfo[] Files = directorio.GetFiles();
            int cont = 1;

            foreach (FileInfo c in Files)
            {
                this.listaArchivoAudio.Add(new ArchivoAudio() { numero = cont, nombre = c.Name, ruta = $"{rutaArchivosAudio}/{c.Name}" });
                cont++;
            }

            //this.listaArchivoAudio.ForEach(c => { Console.WriteLine("nombre: " + c.nombre + "   ruta:" + c.ruta);  });
            this.orientarUsuarioSobreDiscosDisponibles("Bienvenido.");
        }

        private void confirmarUsuarioEscuchando()
        {
            this.reproducirTextoComoSonido("Después del tono, apriete una tecla del teclado para confirmar que me escucha");
        }

        private void orientarUsuarioSobreDiscosDisponibles(string introduccion = "")
        {
            int cantidadDiscos = listaArchivoAudio.Count;
            this.reproducirTextoComoSonido($"{introduccion} Se han encontrado {cantidadDiscos} discos que van desde el uno hasta el {cantidadDiscos}. Después del tono, escriba el número del disco");
        }

        private void synthesizer_SpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            //Console.WriteLine("instrucciones en proceso");
            this.ha_pulsado_tecla = false;  // Previene que el usuario de instrucciones mientras el sistema habla
            this.esta_el_sistema_hablando = true;   // Nuevo
        }
        private void synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            System.Media.SystemSounds.Hand.Play();  // Toca sonido para mostrar al usuario que debe apretar algo

            //Console.WriteLine("instrucciones terminadas");
            this.esta_el_sistema_hablando = false;   // Nuevo
        }

        private void reproducirTextoComoSonido(string texto)
        {
            try
            {
                this.esta_el_sistema_hablando = true;   // Nuevo
                this.ha_pulsado_tecla = false;  // Previene que el usuario de instrucciones mientras el sistema habla

                this.vozInstrucciones.SelectVoice("Microsoft Helena Desktop");
                this.vozInstrucciones.Volume = 100;
                this.vozInstrucciones.Rate = 2;      // Dejar en cero OK.
                this.vozInstrucciones.SpeakProgress += new EventHandler<SpeakProgressEventArgs>(synthesizer_SpeakProgress);
                this.vozInstrucciones.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(synthesizer_SpeakCompleted);
                this.vozInstrucciones.SpeakAsync(texto);
            }
            catch (InvalidOperationException ex) { Console.WriteLine(ex.Message); }
        }

        private void tocarSonido(String rutaSonido)
        {
            this.ha_pulsado_tecla = false;  // Previene que el usuario de instrucciones sin apretar una tecla

            if (this.sonidoPausado == true && this.salidaSonido != null)  // Si estaba pausado y quiere darle play
            {   
                Console.Beep(400, 200); Console.Beep(600, 200); Console.Beep(800, 200);  // Toca pitido 

                this.salidaSonido.Play();
            }
            else
            {
                this.sonidoPausado = false;

                if (this.salidaSonido == null) 
                {
                    Console.Beep(400, 200); Console.Beep(600, 200); Console.Beep(800, 200);  // Toca pitido 

                    this.salidaSonido = new WaveOut();
                    Mp3FileReader mp3FileReader = new Mp3FileReader(rutaSonido);
                    this.salidaSonido.Init(mp3FileReader);
                    this.salidaSonido.Volume = (float) 1.0;   // Volume must be between 0.0 and 1.0
                    this.salidaSonido.Play();
                    //if (this.salidaSonido.PlaybackState.ToString() == "Playing") {  this.sonidoActivo = true;  }
                }
            }
        }

        private void seleccionarDisco(int numeroDisco)
        {
            if (this.listaArchivoAudio.Any(c=> c.numero == numeroDisco))
            {
                this.discoSeleccionado = this.listaArchivoAudio.Where(c => c.numero == numeroDisco).FirstOrDefault();

                String cadena = $"El disco numero {discoSeleccionado.numero} se llama " + discoSeleccionado.nombre + ". Después del tono, para tocar el disco apriete play. Para seleccionar otro disco escriba el número del disco";
                this.reproducirTextoComoSonido(cadena);
            }
            else {
                this.reproducirTextoComoSonido($"No se encontró ningún disco con el número {numeroDisco}. Después del tono, escriba el número de un disco");
            }
        }

        private void apagarComputador(object sender, SpeakCompletedEventArgs e)
        {
            //MessageBox.Show("PC apagado");
            //Console.WriteLine("PC apagado");
            System.Diagnostics.Process.Start("shutdown", "/f /s /t 0");
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (this.esta_el_sistema_hablando == false)
                {
                    if (this.ha_confirmado_el_usuario_que_escucha == false)  // Si el usuario confirma que esta escuchando
                    {
                        this.ha_confirmado_el_usuario_que_escucha = true;
                        this.iniciarCargaArchivosDeAudio();   // Carga los audios e informa al usuario sobre los discos que puede elegir
                        return;
                    }

                    if (e.KeyCode == Keys.Escape)  // Si se apreto escape           if (palabra == "apagar")
                    {
                        if (this.iniciar_proceso_apagar_pc == false)
                        {
                            if (this.salidaSonido != null)
                            {
                                this.salidaSonido.Pause();
                                this.sonidoPausado = true;
                            }

                            this.iniciar_proceso_apagar_pc = true;
                            this.reproducirTextoComoSonido("Después del tono, para apagar el computador apriete nuevamente la tecla escape. Para continuar usando el computador apriete cualquier otra tecla");
                        }
                        else  // Si se confirmo con la palabra apagar
                        {
                            SpeechSynthesizer vozApagar = new SpeechSynthesizer();

                            vozApagar.SelectVoice("Microsoft Helena Desktop");
                            vozApagar.Volume = 100;
                            vozApagar.Rate = 0;
                            vozApagar.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(apagarComputador);
                            vozApagar.SpeakAsync("El computador comenzará a apagarse a la cuenta de tres. Uno, dos, tres");

                            // this.reproducirTextoComoSonido("El computador comenzará a apagarse a la cuenta de tres. Uno, dos, tres");
                            // System.Diagnostics.Process.Start("shutdown", "/f /s /t 0");
                        }

                        return;
                    }

                    this.iniciar_proceso_apagar_pc = false;   // Anula algun posible intento previo de apagar el pc

                    //if (this.existeDiscoSeleccionado == false && e.KeyCode != this.teclaPlay && this.numeroDiscoSeleccionado != "")  
                    if (this.salidaSonido == null && e.KeyCode != this.teclaPlay && this.numeroDiscoSeleccionado != "")  
                    {
                        //int numero = Utilidades.convertirTeclaPulsadaEnNumero(e);
                        int numero = int.Parse(this.numeroDiscoSeleccionado);   // Este valor es definido en el metodo keyDown

                        this.seleccionarDisco(numero);
                        this.existeDiscoSeleccionado = true;
                        this.numeroDiscoSeleccionado = "";  // Borra el numero para que no interfiera
                        return;
                    }

                    if (e.KeyCode == this.teclaPlay && this.existeDiscoSeleccionado == true)  // if (palabra == "play")
                    {
                        //this.reproducirTextoComoSonido("El disco se va a reproducir. Si desea detener el disco diga stop. Para salir de este disco diga exit");
                        //this.tocarSonido("C:/Users/josel/Downloads/Sonidos/probando.mp3");

                        this.tocarSonido(this.discoSeleccionado.ruta);
                        this.sonidoPausado = false;
                    }
                    if (this.salidaSonido != null)
                    {
                        if (e.KeyCode != this.teclaPlay && e.KeyCode != this.teclaStop && e.KeyCode == Keys.Left && e.KeyCode == Keys.Right)
                        {
                            this.reproducirTextoComoSonido("Cuando se reproduce un disco solo se permiten las teclas play y stop");
                        }

                        if (e.KeyCode == this.teclaStop)
                        {
                            if (this.sonidoPausado == false)  // if (palabra == "stop")
                            {
                                this.salidaSonido.Pause();
                                this.sonidoPausado = true;
                                this.reproducirTextoComoSonido("El disco se detuvo. Después del tono, para continuar escuchando apriete play. Para salir de este disco apriete nuevamente la tecla stop");
                                return;
                            }
                            if (this.sonidoPausado == true)
                            {
                                this.salidaSonido.Stop();
                                this.salidaSonido = null;
                                this.existeDiscoSeleccionado = false;
                                this.orientarUsuarioSobreDiscosDisponibles($"Usted ha salido del disco {this.discoSeleccionado.nombre}.");
                                this.discoSeleccionado = null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.numeroDiscoSeleccionado = "";  // Borra el numero para que no interfiera

                string cadena = (ex is ArgumentException) ? ex.Message : "Se encontró un error";
                this.reproducirTextoComoSonido(cadena);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)  // Al pulsar hacia abajo una tecla
        {

            try
            {
                if (this.esta_el_sistema_hablando == false)
                {
                    if (this.salidaSonido != null && (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))  // Si se desea subir o bajar el volumen
                    {
                        if (e.KeyCode == Keys.Left) // Cuando se presione la tecla izquierda
                        {
                            if (this.salidaSonido.Volume > 0.1)
                                this.salidaSonido.Volume = this.salidaSonido.Volume - (float)0.1;
                        }
                        if (e.KeyCode == Keys.Right) // Cuando se presione la tecla derecha
                        {
                            if (this.salidaSonido.Volume < 0.9)
                                this.salidaSonido.Volume = this.salidaSonido.Volume + (float)0.1;
                        }
                    }

                    //if (this.existeDiscoSeleccionado == false)
                    //{
                        if (Utilidades.esTeclaNumerica(e))
                        {
                            int numero = Utilidades.convertirTeclaPulsadaEnNumero(e);

                            if (numero.ToString() != this.numeroDiscoSeleccionado)   // Evitar tecla pulsada
                            {
                                if (this.numeroDiscoSeleccionado != "")  // Si se presiono otro numero antes, junta ambos numeros
                                {
                                    this.numeroDiscoSeleccionado = this.numeroDiscoSeleccionado + numero.ToString();
                                }
                                else
                                {
                                    this.numeroDiscoSeleccionado = numero.ToString();
                                }
                            }
                        }
                        else
                        {
                            this.numeroDiscoSeleccionado = "";  // Borra en caso de que no sea numerico para que no interfiera
                        }
                    //}
                }

            }
            catch (Exception)
            {
                this.numeroDiscoSeleccionado = "";  // Borra el numero en caso de error para que no interfiera
            }
        }

        private void Form1_Deactivate(object sender, EventArgs e)  // Si se ha quitado el foco al programa
        {
            if (this.iniciar_proceso_apagar_pc == false)  // Asi no se activa el mensaje de pantalla roja al apagar el pc
            { 
                this.esta_la_pantalla_seleccionada = false;
                this.BackColor = System.Drawing.Color.Red;   // El color de fondo sera rojo
                //this.reproducirTextoComoSonido("Por favor haga click en la pantalla roja para que vuelva a ser verde");
            }
        }

        private void Form1_Activated(object sender, EventArgs e)  // Si el programa esta en foco nuevamente
        {
            if (this.esta_la_pantalla_seleccionada == false)
            {
                this.esta_la_pantalla_seleccionada = true;
                this.BackColor = System.Drawing.Color.GreenYellow;   // El color de fondo sera verde amarillento
                //this.reproducirTextoComoSonido("La pantalla ha vuelto a ser verde");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)  // Esto se ejecuta cada 10 segundos
        {
            if (this.ha_confirmado_el_usuario_que_escucha == false)  // Si el usuario aun no confirma que esta escuchando
            {
                //Console.WriteLine("reloj click");
                this.confirmarUsuarioEscuchando();   // Lanza un mensaje para ver si el usuario esta escuchando
            }

            if (this.existeDiscoSeleccionado && this.salidaSonido != null && this.salidaSonido.PlaybackState == PlaybackState.Stopped)
            {
                this.sonidoPausado = true;
                this.reproducirTextoComoSonido("El disco ha terminado. Después del tono, apriete la tecla stop para poder elegir otros discos");
            }
        }

    }
}
