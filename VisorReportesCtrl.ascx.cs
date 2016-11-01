using DAL;
using DAL.CFDv32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using BLL;
using Microsoft.Reporting.WebForms;
using System.IO;
using DAL.Enums;
using DAL.ecc11;
using System.Text;
using System.Drawing;
using System.Net;
using PowerGasIntuitum.Helpers;

namespace PowerGasIntuitum.Controles
{
    public partial class VisorReportesCtrl : System.Web.UI.UserControl
    {
        #region Propiedades
        /// <summary>
        /// Ancho del control
        /// </summary>
        public Unit Width
        {
            get { return rptViewer.Width; }
            set { rptViewer.Width = value; }
        }

        /// <summary>
        /// Ancho del control
        /// </summary>
        public Unit Height
        {
            get { return rptViewer.Height; }
            set { rptViewer.Height = value; }
        }

        /// <summary>
        /// Obtiene el PDF del reporte que se está mostrando
        /// </summary>
        public byte[] PDF
        {
            get
            {
                return rptViewer.LocalReport.Render("PDF");
            }
        }

        /// <summary>
        /// Obtiene el Excel del reporte que se está mostrando
        /// </summary>
        public byte[] Excel
        {
            get
            {
                return rptViewer.LocalReport.Render("Excel");
            }
        }

        /// <summary>
        /// Obtiene la representacion en texto plano que se está mostrando
        /// </summary>
        public string TextoPlano
        {
            get
            {
                string rval = "";
                if (string.IsNullOrEmpty(CurrentReport))
                    return null;
                switch (CurrentReport)
                {
                    case "ConsumoEsRpt":
                        var datosCE = rptViewer.LocalReport.DataSources["ReporteDS"].Value as List<VistaReporteConsumosEstaciones>;
                        rval =
                            "No Estacion|Nombre|Serie Terminal|Folio Venta|Folio Tarjeta|Combustible|Fecha Venta|Precio Litro|Litros|Total\r\n" +
                            datosCE.Select(d =>
                        $"{d.NoEstacion}|{d.Nombre}|{d.SerieTerminal}|{d.FolioVenta}|{d.FolioTarjeta}|{d.Combustible}|{d.FechaVenta:dd/MM/yyyy HH:mm:ss}|{d.PrecioLitro:0.00}|{d.Litros:0.000}|{d.Total:0.00}\r\n")
                        .Aggregate((a, b) => a + b);
                        break;
                    case "OrdenesPagoRpt":
                        var datosOP = rptViewer.LocalReport.DataSources["ReporteDS"].Value as List<VistaReporteOrdenesPago>;
                        rval =
                             "No Estacion|Nombre|Folio OP|Fecha Generada|Operaciones|Reembolso|Participacion|Mantenimiento|Estatus|Fecha Pago|Folio Bancario|Fecha Inicial|Fecha Final\r\n" +
                             datosOP.Select(d =>
                         $"{d.NoEstacion}|{d.Nombre}|{d.FolioOP}|{d.FechaGenerada:dd/MM/yyyy HH:mm:ss}|{d.Operaciones:0.00}|{d.Reembolso:0.00}|{d.Participacion:0.00}|{d.Mantenimiento:0.00}|{d.Estatus}|{d.FechaPago:dd/MM/yyyy HH:mm:ss}|{d.FolioBancario}|{d.FechaInicial:dd/MM/yyyy}|{d.FechaFinal:dd/MM/yyyy}\r\n")
                         .Aggregate((a, b) => a + b);
                        break;
                }
                return rval;
            }
        }

        /// <summary>
        /// Obtiene o establece el nombre del reporte que se está mostrando
        /// </summary>
        private string CurrentReport
        {
            get
            {
                if (ViewState["CurrentReport"] == null)
                    ViewState["CurrentReport"] = "";
                return (string)ViewState["CurrentReport"];
            }
            set
            {
                ViewState["CurrentReport"] = value;
            }
        }
        #endregion

        #region Métodos

        protected void Page_Load(object sender, EventArgs e)
        {
            rptViewer.KeepSessionAlive = true;

            if (!IsPostBack)
            {

            }
        }

        public void LoadReporteSeguimientoCRM(DateTime? fechaInicio = null, DateTime? fechaFin = null, int idAsesor = 0, int idProspecto = 0)
        {
            if (!fechaInicio.HasValue) { fechaInicio = new DateTime(2016, 01, 01); }
            if (!fechaFin.HasValue) { fechaFin = new DateTime(2099, 01, 01); }

            var lista = ProspectosBLL.GetSeguimiento(Convert.ToDateTime(fechaInicio), Convert.ToDateTime(fechaFin), idAsesor, idProspecto);
            string periodo = Convert.ToDateTime(fechaInicio).ToShortDateString() + "-" + Convert.ToDateTime(fechaFin).ToShortDateString();

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/SeguimientoCRM.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }

            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", lista));
            rptViewer.LocalReport.SetParameters(new ReportParameter("Periodo", periodo));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadOpTesoreria(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            if (!fechaInicio.HasValue) { }
            if (!fechaFin.HasValue) { }
            var lista = OrdenPagoTesoreriaBLL.CalculaOpTesoreria(Convert.ToDateTime(fechaInicio), Convert.ToDateTime(fechaFin));
            decimal totalBono = 0, totalPesos = 0, totalLitros = 0;
            string periodo = Convert.ToDateTime(fechaInicio).ToShortDateString() + "-" + Convert.ToDateTime(fechaFin).ToShortDateString();

            totalBono = Convert.ToDecimal(lista.Sum(v => v.Bonificacion));
            totalPesos = Convert.ToDecimal(lista.Sum(v => v.TotalPesos));
            totalLitros = Convert.ToDecimal(lista.Sum(v => v.TotalLitros));

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/OPTesoreria.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }

            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", lista));
            rptViewer.LocalReport.SetParameters(new ReportParameter("Periodo", periodo));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalPeriodo", totalBono.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalAbonos", totalBono.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalPesos", totalPesos.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalLitros", totalLitros.ToString("N")));
            rptViewer.LocalReport.Refresh();
        }


        public void LoadReportePagosEC(int idCliente, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var lista = ReportesBLL.GetReportePagosParcialesEC(idCliente, fechaInicio, fechaFin);

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/PagosParcialesEC.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }

            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", lista));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicio.ToString()));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFin.ToString()));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadBuroCreditoMorales(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var lista = ReportesBLL.GetReporteBuroMorales(fechaInicio, fechaFin);

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/BuroMoral.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }

            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", lista));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicio.ToString()));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFin.ToString()));
            rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", "Sistema"));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadBuroCreditoFisicas(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var lista = ReportesBLL.GetReporteBuroFisica(fechaInicio, fechaFin);

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/BuroFisica.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }

            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", lista));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicio.ToString()));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFin.ToString()));
            rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", "Sistema"));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadEstadoCuentaEstacionReport(int idEstacion, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            decimal saldoInicial = 0, saldoFinal = 0;
            decimal totalAbonos = 0, totalAbonosCancelados = 0, totalCargos = 0, totalPeriodo = 0;

            var abonos = ReportesBLL.GetCargasPorEstacion(idEstacion, fechaInicio, fechaFin);
            var abonosCancelados = ReportesBLL.GetCargasPorEstacion(idEstacion, fechaInicio, fechaFin, false);
            var cargos = ReportesBLL.GetOrdenesPagoEstacion(idEstacion, fechaInicio, fechaFin);
            var cliente = EstacionesBLL.ReadEstacion(idEstacion);
            DateTime fechaFinal = new DateTime();

            if (fechaInicio.HasValue)
            {
                fechaFinal = Convert.ToDateTime(fechaInicio);
                fechaFinal = fechaFinal.AddDays(-1);
                saldoInicial = ReportesBLL.GetSaldoInicialEdoCuentaEstacion(idEstacion, fechaFinal);
            }
            else
            {
                saldoInicial = 0;
            }

            if (abonos.Count > 0) { totalAbonos = Convert.ToDecimal(abonos.Sum(v => v.Total)); }
            if (abonosCancelados.Count > 0) { totalAbonosCancelados = Convert.ToDecimal(abonosCancelados.Sum(v => v.Total)); }
            if (cargos.Count > 0) { totalCargos = Convert.ToDecimal(cargos.Sum(v => v.TotalOperaciones)); }

            foreach (var ac in abonosCancelados)
            {
                ac.Total = (-1) * ac.Total;
                abonos.Add(ac);
            }


            totalAbonos += (-1 * totalAbonosCancelados);
            saldoFinal = (saldoInicial + totalAbonos) - (totalCargos);
            totalPeriodo = totalAbonos - totalCargos;

            string paramCliente = "No. " + cliente.NoEstacion.ToString() + " " + cliente.Nombre;
            string paramFecha = fechaInicio.ToString() + " - " + fechaFin.ToString();
            string paramSaldoInicial = Math.Round(saldoInicial, 2).ToString("N");
            string paramSaldoFinal = Math.Round(saldoFinal, 2).ToString("N");

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/EdoCuentaEstacion.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }
            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("AbonosDS", abonos));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("CargosDS", cargos));
            rptViewer.LocalReport.SetParameters(new ReportParameter("Cliente", paramCliente));
            rptViewer.LocalReport.SetParameters(new ReportParameter("Periodo", paramFecha));
            rptViewer.LocalReport.SetParameters(new ReportParameter("SaldoInicial", paramSaldoInicial));
            rptViewer.LocalReport.SetParameters(new ReportParameter("SaldoFinal", paramSaldoFinal));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalPeriodo", totalPeriodo.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalAbonos", totalAbonos.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalCargos", (totalCargos).ToString("N")));
            rptViewer.LocalReport.Refresh();

        }

        public void LoadEstadoCuentaClienteReport(int idCliente, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            decimal saldoInicial = 0, saldoFinal = 0;
            decimal totalAbonos = 0, totalCargos = 0, totalPeriodo = 0, totalCargosComision = 0;
            DateTime fechaFinal = new DateTime();
            var abonos = ReportesBLL.GetDepositosPorCliente(idCliente, fechaInicio, fechaFin);
            var cargos = ReportesBLL.GetCargosPorClientePorEstacion(idCliente, fechaInicio, fechaFin);
            var cargosComision = ReportesBLL.GetCargosPorComisionNuevo(idCliente, fechaInicio, fechaFin);
            var cliente = ClientesBLL.ReadCliente(idCliente);
            List<SPGetPagosECporCliente_Result> pagos = new List<SPGetPagosECporCliente_Result>();

            if (cliente.IdTipoCliente == 3)
            {
                //if(cliente.Cuentas.FirstOrDefault().SaldoPrepago>1)
                //{
                abonos = new List<SPGetDepositosPorCliente_Result>();

                pagos = ReportesBLL.GetPagosEcPorCliente(idCliente, fechaInicio, fechaFin);
                foreach (var p in pagos)
                {

                    SPGetDepositosPorCliente_Result tmp = new SPGetDepositosPorCliente_Result();
                    tmp.Concepto = p.Concepto;
                    tmp.Deposito = p.Deposito;
                    tmp.Acreditacion = p.Fecha;
                    //var a = abonos.FirstOrDefault(v => v.Deposito == tmp.Deposito && v.Concepto==tmp.Concepto);
                    //if (a == null) {
                    abonos.Add(tmp); //}
                                     //}
                                     //}
                }
            }

            abonos = abonos.OrderBy(a => a.Acreditacion).ToList();

            if (fechaInicio.HasValue)
            {
                fechaFinal = Convert.ToDateTime(fechaInicio);
                fechaFinal = fechaFinal.AddDays(-1);
                saldoInicial = ReportesBLL.GetSaldoInicial(idCliente, fechaFinal);
            }
            else
            {
                saldoInicial = 0;
            }

            if (abonos.Count > 0) { totalAbonos = abonos.Sum(v => v.Deposito); }
            if (cargos.Count > 0) { totalCargos = Convert.ToDecimal(cargos.Where(v => v.Total.HasValue == true).Sum(v => v.Total)); }
            if (cargosComision.Count > 0) { totalCargosComision = Convert.ToDecimal(cargosComision.Sum(v => v.Total)); }

            totalCargos += totalCargosComision;
            if (cliente.IdTipoCliente == 2)
            {
                saldoFinal = (saldoInicial + totalAbonos) - (totalCargos);
            }
            if (cliente.IdTipoCliente == 3)
            {
                saldoFinal = (saldoInicial + totalCargos) - (totalAbonos);
            }

            string paramCliente = "No. " + cliente.IdCliente.ToString() + " " + cliente.Cliente;
            string paramFecha = fechaInicio.ToString() + " - " + fechaFin.ToString();
            string paramSaldoInicial = Math.Round(saldoInicial, 2).ToString("N");
            string paramSaldoFinal = Math.Round(saldoFinal, 2).ToString("N");

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/EstadoCuentaContable.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }
            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("AbonosDS", abonos));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("CargosDS", cargos));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("CargosComisionDS", cargosComision));
            rptViewer.LocalReport.SetParameters(new ReportParameter("Cliente", paramCliente));
            rptViewer.LocalReport.SetParameters(new ReportParameter("Periodo", paramFecha));
            rptViewer.LocalReport.SetParameters(new ReportParameter("SaldoInicial", paramSaldoInicial));
            rptViewer.LocalReport.SetParameters(new ReportParameter("SaldoFinal", paramSaldoFinal));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalPeriodo", totalPeriodo.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalAbonos", totalAbonos.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("TotalCargos", (totalCargos).ToString("N")));
            rptViewer.LocalReport.Refresh();

        }


        public void LoadPreCalculoOpAsesor(string asesorOp, string fechaLimite, List<SpGetVentasConsumosAsesoresPorcentaje_Result> lista, List<ClienteCobranzaExterna> penalizacion, decimal descuento)
        {
            decimal totalComisiones = 0, totalPenalizaciones = 0, totalFinal = 0;

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/ComisionesAsesor.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }

            if (lista.Count > 0)
            {
                totalComisiones = Convert.ToDecimal(lista.Sum(v => v.ComisionAsesor));
            }
            if (penalizacion.Count > 0)
            {
                totalPenalizaciones = Convert.ToDecimal(penalizacion.Sum(p => p.Penalizacion));
            }

            totalFinal = totalComisiones - descuento;

            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", lista));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("PenalizacionDS", penalizacion));
            rptViewer.LocalReport.SetParameters(new ReportParameter("asesor", asesorOp));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaLimite));

            rptViewer.LocalReport.SetParameters(new ReportParameter("totalComisiones", totalComisiones.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("totalPenalizacion", totalPenalizaciones.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("totalFinal", totalFinal.ToString("N")));

            rptViewer.LocalReport.SetParameters(new ReportParameter("descuentoComision", descuento.ToString("N")));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadOpAsesorExterno(string asesorOp, string fechaLimite, List<SpGetVentasConsumosAsesoresPorcentajeOP_Result> lista, decimal totalPenalizaciones, decimal descuento)
        {
            decimal totalComisiones = 0, totalFinal = 0;

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/OrdenPagoAsesor.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }

            if (lista.Count > 0)
            {
                totalComisiones = Convert.ToDecimal(lista.Sum(v => v.ComisionAsesor));
            }

            totalFinal = totalComisiones - descuento;

            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", lista));
            rptViewer.LocalReport.SetParameters(new ReportParameter("asesor", asesorOp));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaLimite));

            rptViewer.LocalReport.SetParameters(new ReportParameter("totalComisiones", totalComisiones.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("totalPenalizacion", totalPenalizaciones.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("totalFinal", totalFinal.ToString("N")));

            rptViewer.LocalReport.SetParameters(new ReportParameter("descuentoComision", descuento.ToString("N")));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadOpAsesor(string asesorOp, string fechaLimite, List<SpGetVentasConsumosAsesoresPorcentajeOP_Result> lista, decimal totalPenalizaciones, decimal descuento)
        {
            decimal totalComisiones = 0, totalFinal = 0;

            using (FileStream fs = File.OpenRead(MapPath("~/Reports/OrdenPagoAsesor.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }

            if (lista.Count > 0)
            {
                totalComisiones = Convert.ToDecimal(lista.Sum(v => v.ComisionAsesor));
            }

            totalFinal = totalComisiones - descuento;

            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", lista));
            rptViewer.LocalReport.SetParameters(new ReportParameter("asesor", asesorOp));
            rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaLimite));

            rptViewer.LocalReport.SetParameters(new ReportParameter("totalComisiones", totalComisiones.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("totalPenalizacion", totalPenalizaciones.ToString("N")));
            rptViewer.LocalReport.SetParameters(new ReportParameter("totalFinal", totalFinal.ToString("N")));

            rptViewer.LocalReport.SetParameters(new ReportParameter("descuentoComision", descuento.ToString("N")));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadReporteIngresosReport(List<int> seleccion, int idUsuario, DateTime? fechaInicial = null, DateTime? fechaFinal = null)
        {
            var reporte = ReportesBLL.GetReporteIngresos(seleccion, fechaInicial, fechaFinal);
            if (HayResultados(reporte.Count))
            {
                var usuario = "";

                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }

                using (FileStream fs = File.OpenRead(MapPath("~/Reports/Ingresos.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadEstadoCuentaReport(int idEstadoCuenta)
        {
            var ec = EstadosCuentaBLL.GetEstadoCuentaById(idEstadoCuenta);
            Comprobante comprobante = CFDIsBLL.GetCFD(ec.IdCFDI).First();
            TimbreFiscalDigital timbref = CFDIsBLL.GetTFD(comprobante);
            EstadoDeCuentaCombustible ecc = CFDIsBLL.GetComplementoECC(comprobante);
            ImagenQR imagen = ImagenQR.CreaNueva(comprobante, timbref);
            string cadenaOriginal = CFDIsBLL.GetCadenaOriginal(ec.IdCFDI);
            string cantidadConLetra = Helpers.CantidadesHelper.CantidadConLetra(comprobante.total);
            var metodoPago = MetodosPagoBLL.GetMetodoByClaveSAT(comprobante.metodoDePago);
            using (FileStream fs = File.OpenRead(System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/EstadoCuentaRpt.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }
            //rptViewer.LocalReport.LoadReportDefinition
            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ComprobanteDS", new Comprobante[] { comprobante }));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("TimbreFiscalDigitalDS", new TimbreFiscalDigital[] { timbref }));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ConceptosDS", CFDIsBLL.GetConceptosComplementoECC(comprobante)));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImpuestosDS", comprobante.Impuestos.Traslados));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("EstadoCuentaImpresionDS", EstadosCuentaBLL.GetDatosImpresion(idEstadoCuenta)));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ComisionDS", EstadosCuentaBLL.GetComision(idEstadoCuenta)));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("DetallesEcDS", EstadosCuentaBLL.GetDetallesEC(idEstadoCuenta)));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenQRDS", new ImagenQR[] { imagen }));
            rptViewer.LocalReport.SetParameters(new ReportParameter("cadenaOriginal", cadenaOriginal));
            rptViewer.LocalReport.SetParameters(new ReportParameter("cantidadConLetra", cantidadConLetra));
            rptViewer.LocalReport.SetParameters(new ReportParameter("metodoPago", metodoPago != null ? metodoPago.MetodoPago : ""));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadFormatoCaratulaPF(int idClt)
        {
            var datos = ClientesBLL.GetDatosGenerales(idClt);
            var cte = ClientesBLL.GetClienteByID(idClt);
            ImagenLogo imgLogo;
            string nomCaratula = "", email;
            if (HayResultados(datos.Count))
            {
                email = UsuariosBLL.GetUsuariosByIdCliente(idClt).FirstOrDefault().Email;
                if (cte.IdTipoCliente == 2)
                {
                    if (cte.TipoPersona == "F")
                        nomCaratula = "CaratulaPrepagoPF.rdlc";
                    else
                        nomCaratula = "CaratulaPrepagoPM.rdlc";
                }
                else
                {
                    if (cte.TipoPersona == "F")
                        nomCaratula = "CaratulaCreditoPF.rdlc";
                    else
                        nomCaratula = "CaratulaCreditoPM.rdlc";
                }

                using (FileStream fs = File.OpenRead(MapPath("~/Formats/" + nomCaratula)))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                rptViewer.LocalReport.EnableHyperlinks = true;
                rptViewer.HyperlinkTarget = "_blank";
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("DatosGeneralesDS", datos));
                rptViewer.LocalReport.SetParameters(new ReportParameter("email", email));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadFormatoCartaPF(int idClt)
        {
            var datos = ClientesBLL.GetDatosGenerales(idClt);
            var cte = ClientesBLL.GetClienteByID(idClt);
            string nomCarta = "", email;
            if (HayResultados(datos.Count))
            {
                if (cte.TipoPersona == "F")
                    nomCarta = "CartaDatosGeneralesPF.rdlc";
                else
                    nomCarta = "CartaDatosGeneralesPM.rdlc";
                email = UsuariosBLL.GetUsuariosByIdCliente(idClt).FirstOrDefault().Email;
                using (FileStream fs = File.OpenRead(MapPath("~/Formats/" + nomCarta)))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.EnableHyperlinks = true;
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("DatosGeneralesDS", datos));
                rptViewer.LocalReport.SetParameters(new ReportParameter("email", email));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadFacturaReport(int idFactura)
        {
            Comprobante comprobante = CFDIsBLL.GetCFD(idFactura).First();
            TimbreFiscalDigital timbref = CFDIsBLL.GetTFD(comprobante);
            ImagenQR imagen = ImagenQR.CreaNueva(comprobante, timbref);
            InfoFiscalReporte receptor = InfoFiscalBLL.GetInfoFiscalRpt(CFDIsBLL.GetCFDiById(idFactura).IdInfoFiscalReceptor).FirstOrDefault();
            string cadenaOriginal = CFDIsBLL.GetCadenaOriginal(idFactura);
            string cantidadConLetra = Helpers.CantidadesHelper.CantidadConLetra(comprobante.total);
            var metodoPago = MetodosPagoBLL.GetMetodoByClaveSAT(comprobante.metodoDePago);
            using (FileStream fs = File.OpenRead(MapPath("~/Reports/FacturaRpt.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }
            //rptViewer.LocalReport.LoadReportDefinition
            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ComprobanteDS", new Comprobante[] { comprobante }));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReceptorDS", new InfoFiscalReporte[] { receptor }));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("TimbreFiscalDigitalDS", new TimbreFiscalDigital[] { timbref }));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ComprobanteConceptoDS", comprobante.Conceptos));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenQRDS", new ImagenQR[] { imagen }));
            rptViewer.LocalReport.SetParameters(new ReportParameter("cadenaOriginal", cadenaOriginal));
            rptViewer.LocalReport.SetParameters(new ReportParameter("cantidadConLetra", cantidadConLetra));
            rptViewer.LocalReport.SetParameters(new ReportParameter("metodoPago", metodoPago != null ? metodoPago.MetodoPago : ""));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadEdoCtaClientesGlobal(DateTime fechaInicial, DateTime fechaFinal)
        {
            var reporte = ReportesBLL.GetReporteEstadoCuentaClientesGlobal(fechaInicial, fechaFinal);
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                var usr = UsuariosBLL.ReadUsuario(Global.CurrentUser.IdUsuario);
                usuario = string.Format("{0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/EstadoCuentaClientesGlobal.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("Usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadComparativoConsumosClientes(DateTime fechaInicial, DateTime fechaFinal, List<int> idsAsesores, string criterio)
        {
            var reporte = ReportesBLL.GetReporteComparativoConsumosClientes(fechaInicial, fechaFinal, idsAsesores, Convert.ToInt32(criterio));
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                var usr = UsuariosBLL.ReadUsuario(Global.CurrentUser.IdUsuario);
                usuario = string.Format("{0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/CompConsumosRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadInformacionEstacionesReport(int idEst, int idMun, int idUsuario)
        {
            var estaciones = ReportesBLL.GetReporteEstacionesCliente(idEst, idMun);
            MemoryStream ms;
            ImagenLogo imgLogo;
            string nomUser = "", imagePath = "", ext = "", reportPath = "";
            string logoFileName = "default.png";
            if (HayResultados(estaciones.Count))
            {
                var usuario = UsuariosBLL.GetUsuarioByIdUsuario(idUsuario);
                nomUser = usuario.Nombre + " " + usuario.APaterno;
                reportPath = "~/Reports/EstacionesClienteRpt.rdlc";
                using (FileStream fs = File.OpenRead(MapPath(reportPath)))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                var c = ClientesBLL.GetClienteByUsuario(idUsuario);
                if (c != null)
                    if (c.ImagenLogo != null)
                        logoFileName = c.ImagenLogo;

                rptViewer.LocalReport.EnableExternalImages = true;
                imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                ms = FileHelper.GetFileStream(logoFileName, TipoArchivoEnum.ImagenLogoUsuario, out ext);
                imgLogo = new ImagenLogo();
                imgLogo.Bytes = ms.ToArray();

                rptViewer.LocalReport.EnableHyperlinks = true;
                rptViewer.HyperlinkTarget = "_blank";
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("dsEstacionesCliente", estaciones));
                rptViewer.LocalReport.SetParameters(new ReportParameter("Usuario", nomUser));
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenLogoDS", new ImagenLogo[] { imgLogo }));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadOrdenPago(int idOp, int idFactura)
        {
            Comprobante comprobante = CFDIsBLL.GetCFD(idFactura).First();
            TimbreFiscalDigital timbref = CFDIsBLL.GetTFD(comprobante);
            ImagenQR imagen = ImagenQR.CreaNueva(comprobante, timbref);
            InfoFiscalReporte receptor = InfoFiscalBLL.GetInfoFiscalRpt(CFDIsBLL.GetCFDiById(idFactura).IdInfoFiscalReceptor).FirstOrDefault();
            List<VistaReporteConsumosEstaciones> conceptosOp = ReportesBLL.GetReporteConsumosOP(idOp);
            string cadenaOriginal = CFDIsBLL.GetCadenaOriginal(idFactura);
            string cantidadConLetra = Helpers.CantidadesHelper.CantidadConLetra(comprobante.total);
            var metodoPago = MetodosPagoBLL.GetMetodoByClaveSAT(comprobante.metodoDePago);
            using (FileStream fs = File.OpenRead(System.Web.Hosting.HostingEnvironment.MapPath("~/Reports/OrdenPagoRpt.rdlc")))
            {
                rptViewer.LocalReport.LoadReportDefinition(fs);
            }
            //rptViewer.LocalReport.LoadReportDefinition
            rptViewer.LocalReport.DataSources.Clear();
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ComprobanteDS", new Comprobante[] { comprobante }));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReceptorDS", new InfoFiscalReporte[] { receptor }));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("TimbreFiscalDigitalDS", new TimbreFiscalDigital[] { timbref }));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ComprobanteConceptoDS", comprobante.Conceptos));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ConceptosOPDS", conceptosOp));
            rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenQRDS", new ImagenQR[] { imagen }));
            rptViewer.LocalReport.SetParameters(new ReportParameter("cadenaOriginal", cadenaOriginal));
            rptViewer.LocalReport.SetParameters(new ReportParameter("cantidadConLetra", cantidadConLetra));
            rptViewer.LocalReport.SetParameters(new ReportParameter("metodoPago", metodoPago != null ? metodoPago.MetodoPago : ""));
            rptViewer.LocalReport.Refresh();
        }

        public void LoadFacturasReportes(int idUsuario, DateTime? fechaInicial = null, DateTime? fechaFinal = null, string filtroEstatus = "")
        {
            var facturas = ReportesBLL.GetReporteFacturas(fechaInicial, fechaFinal, filtroEstatus);
            if (HayResultados(facturas.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/FacturasRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                string fi = "", ff = "";
                if (fechaFinal == null)
                {
                    ff = "";
                }
                else
                {
                    ff = fechaFinal.ToString();
                }
                if (fechaInicial == null)
                {
                    fi = "";
                }
                else
                {
                    fi = fechaInicial.ToString();
                }

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", facturas));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fi));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", ff));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadComparativoMensual(int anio)
        {

            var reporte = ReportesBLL.GetReporteTotalVentasClienteMensual(anio);
            if (HayResultados(reporte.Count))
            {

                using (FileStream fs = File.OpenRead(MapPath("~/Reports/TotalMensualCliente.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadReporteOP(int idUsuario, DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idEstacionSeleccionada = 0)
        {
            int idEstacion = EstacionesBLL.GetIdEstacionByUsuario(idUsuario);
            if (idEstacion == 0 && idEstacionSeleccionada != 0)
            {
                idEstacion = idEstacionSeleccionada;
            }

            var reporte = ReportesBLL.GetReporteOrdenesPago(idEstacion, fechaInicial, fechaFinal, idUsuario);
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/OrdenesPagoRpt.rdlc")))
                {
                    CurrentReport = "OrdenesPagoRpt";
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                string fi = "", ff = "";
                if (fechaFinal == null)
                {
                    ff = "";
                }
                else
                {
                    ff = fechaFinal.ToString();
                }
                if (fechaInicial == null)
                {
                    fi = "";
                }
                else
                {
                    fi = fechaInicial.ToString();
                }

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fi));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", ff));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadReporteAutorizaciones(int idUsuario, DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idEstacion = 0)
        {
            var reporte = ReportesBLL.GetReporteAutorizaciones(fechaInicial, fechaFinal, idEstacion);
            if (HayResultados(reporte.Count))
            {
                var porcentaje = ReportesBLL.GetPorcentajeAutorizaciones(fechaInicial, fechaFinal, idEstacion);
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }

                using (FileStream fs = File.OpenRead(MapPath("~/Reports/AutorizacionesRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                string fi = "", ff = "";
                if (fechaFinal == null)
                {
                    ff = "";
                }
                else
                {
                    ff = fechaFinal.ToString();
                }
                if (fechaInicial == null)
                {
                    fi = "";
                }
                else
                {
                    fi = fechaInicial.ToString();
                }

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("Porcentaje", porcentaje));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fi));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", ff));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadConsumoOP(int idUsuario, int idOp, DateTime? fechaInicial = null, DateTime? fechaFinal = null)
        {
            var reporte = ReportesBLL.GetReporteConsumosOP(idOp);
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/ConsumoEsRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                string fi = "", ff = "";
                if (fechaFinal == null)
                {
                    ff = "";
                }
                else
                {
                    ff = fechaFinal.ToString();
                }
                if (fechaInicial == null)
                {
                    fi = "";
                }
                else
                {
                    fi = fechaInicial.ToString();
                }

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fi));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", ff));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadConsumoEs(int idUsuario, bool filtroRngCons, DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idEstacion = 0, bool rngLitros = false, int? rngIni = null, int? rngFin = null, List<string> combustibles = null, List<int> turnos = null)
        {
            var reporte = ReportesBLL.GetReporteConsumosEstaciones(filtroRngCons, fechaInicial, fechaFinal, idEstacion, combustibles, turnos, idUsuario, rngLitros, rngIni, rngFin);
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/ConsumoEsRpt.rdlc")))
                {
                    CurrentReport = "ConsumoEsRpt";
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                string fi = "", ff = "";
                if (fechaFinal == null)
                {
                    ff = "";
                }
                else
                {
                    ff = fechaFinal.ToString();
                }
                if (fechaInicial == null)
                {
                    fi = "";
                }
                else
                {
                    fi = fechaInicial.ToString();
                }
                string turnosString = "";
                if (turnos != null)
                    foreach (var idTurno in turnos)
                    {
                        Turnos t = TurnosBLL.GetTurnoById(idTurno: idTurno);
                        turnosString += t.Nombre + ",";
                    }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fi));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", ff));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.SetParameters(new ReportParameter("turnos", turnosString));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadSaldoTarjetas(int idUsuario, int idCliente, DateTime? fechaInicial = null, DateTime? fechaFinal = null)
        {
            var reporte = ReportesBLL.GetReporteSaldoTarjetas(idCliente, fechaInicial, fechaFinal);
            if (HayResultados(reporte.Count))
            {
                string logoFileName = "default.png";
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);

                    var c = ClientesBLL.GetClienteByUsuario(idUsuario);

                    if (c != null)
                    {
                        if (c.ImagenLogo != null)
                        {
                            logoFileName = c.ImagenLogo;
                        }
                    }
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/SaldoTarjetasRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                string fi = "", ff = "";
                if (fechaFinal == null)
                {
                    ff = "";
                }
                else
                {
                    ff = fechaFinal.ToString();
                }
                if (fechaInicial == null)
                {
                    fi = "";
                }
                else
                {
                    fi = fechaInicial.ToString();
                }

                rptViewer.LocalReport.EnableExternalImages = true;

                //logoFileName = "185f433613c7b1d1c.jpg";
                string ext = "";
                MemoryStream ms = FileHelper.GetFileStream(logoFileName, TipoArchivoEnum.ImagenLogoUsuario, out ext);
                //string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                ImagenLogo imgLogo = new ImagenLogo();
                imgLogo.Bytes = ms.ToArray();

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fi));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", ff));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.SetParameters(new ReportParameter("pathLogo", " "));
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenLogoDS", new ImagenLogo[] { imgLogo }));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadDetalleSaldoTarjetas(int idUsuario, int idTarjeta, DateTime? fechaInicial = null, DateTime? fechaFinal = null)
        {
            var reporte = ReportesBLL.GetDetalleSaldoTarjetas(idTarjeta, fechaInicial, fechaFinal);
            string logoFileName = "default.png";
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);

                    var c = ClientesBLL.GetClienteByUsuario(idUsuario);

                    if (c != null)
                    {
                        if (c.ImagenLogo != null)
                        {
                            logoFileName = c.ImagenLogo;
                        }
                    }
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/DetalleSaldoTarjetasRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                string fi = "", ff = "";
                if (fechaFinal == null)
                {
                    ff = "";
                }
                else
                {
                    ff = fechaFinal.ToString();
                }
                if (fechaInicial == null)
                {
                    fi = "";
                }
                else
                {
                    fi = fechaInicial.ToString();
                }

                rptViewer.LocalReport.EnableExternalImages = true;
                string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                string ext = "";
                MemoryStream ms = FileHelper.GetFileStream(logoFileName, TipoArchivoEnum.ImagenLogoUsuario, out ext);
                //string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                ImagenLogo imgLogo = new ImagenLogo();
                imgLogo.Bytes = ms.ToArray();

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fi));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", ff));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.SetParameters(new ReportParameter("pathLogo", imagePath));
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenLogoDS", new ImagenLogo[] { imgLogo }));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadReporteGlobalClientes(int idUsuario, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var reporte = ReportesBLL.GetReporteGlobalClientes(fechaInicio, fechaFin);
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/SaldoClientesGlobal.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadReporteCreditoGeneralCobranzaExterna(int idUsuario)
        {
            var reporte = ReportesBLL.GetReporteCreditoCobranzaExternaV2();
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/CreditosGeneral.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadReporteCreditoGeneral(int idUsuario)
        {
            var reporte = ReportesBLL.GetReporteCreditoSinCobranzaExternaV2();
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/CreditosGeneral.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadReporteCredito(int idUsuario, int idCliente)
        {
            var reporte = ReportesBLL.GetReporteCreditos(idCliente);
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/Creditos.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ReporteDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadRendimientos(int idUsuario, int idCliente, DateTime? fechaInicio = null, DateTime? fechaFin = null, int idCentroCostos = 0, int idVehiculo = 0)
        {
            var reporte = ReportesBLL.GetReporteRendimientosNuevo(idCliente, fechaInicio, fechaFin, idCentroCostos, idVehiculo);
            if (HayResultados(reporte.Count))
            {
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/RendimientosRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                string logoFileName = "default.png";
                if (idCliente > 0)
                {
                    var c = ClientesBLL.ReadCliente(idCliente);
                    if (c != null)
                    {
                        if (c.ImagenLogo != null)
                        {
                            logoFileName = c.ImagenLogo;
                        }
                    }
                }
                rptViewer.LocalReport.EnableExternalImages = true;
                string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                string ext = "";
                MemoryStream ms = FileHelper.GetFileStream(logoFileName, TipoArchivoEnum.ImagenLogoUsuario, out ext);
                //string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                ImagenLogo imgLogo = new ImagenLogo();
                imgLogo.Bytes = ms.ToArray();

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("RendimientoDS", reporte));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.SetParameters(new ReportParameter("pathLogo", imagePath));
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenLogoDS", new ImagenLogo[] { imgLogo }));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadVentasCanceladasReport(DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idCliente = 0, int idEstacion = 0, int idUsuario = 0, string folioCancelacion = null)
        {
            var ventas = VentasBLL.GetVentasCanceladas(fechaInicial, fechaFinal, idCliente, idEstacion, idUsuario, folioCancelacion);
            if (HayResultados(ventas.Count))
            {
                var estacion = idEstacion > 0 ? "Estacion: " + EstacionesBLL.GetEstacionById(idEstacion).Nombre : "";
                var cliente = idCliente > 0 ? "Cliente: " + ClientesBLL.ReadCliente(idCliente).Cliente : "";
                var usuario = "";
                if (idUsuario > 0)
                {
                    var usr = UsuariosBLL.ReadUsuario(idUsuario);
                    usuario = string.Format("Usuario: {0} {1} {2}", usr.Nombre, usr.APaterno, usr.AMaterno);
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/VentasCanceladasRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("VentasCanceladasDS", ventas));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("cliente", cliente));
                rptViewer.LocalReport.SetParameters(new ReportParameter("estacion", estacion));
                rptViewer.LocalReport.SetParameters(new ReportParameter("usuario", usuario));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadConsumosClientesReport(DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idCliente = 0, List<int> idsEstaciones = null, List<int> idsTarjetas = null, List<int> combustibles = null, bool? autorizaciones = null, List<int> idsCentrosCostos = null, bool? totales = null, List<int> idsAsesores = null)
        {
            var ventas = VentasBLL.GetVentasActivasDetalle(fechaInicial, fechaFinal, idCliente, idsEstaciones, idsTarjetas, combustibles, autorizaciones, idsCentrosCostos, idsAsesores);
            //var estacion = idEstacion > 0 ? "Estacion: " + EstacionesBLL.GetEstacionById(idEstacion).Nombre : "";
            if (HayResultados(ventas.Count))
            {
                var cliente = idCliente > 0 ? "Cliente: " + ClientesBLL.ReadCliente(idCliente).Cliente : "";
                var reportPath = idCliente > 0 ? "~/Reports/ConsumosClienteRpt.rdlc" : "~/Reports/ConsumosClienteGlobalRpt.rdlc";
                using (FileStream fs = File.OpenRead(MapPath(reportPath)))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }


                string logoFileName = "default.png";

                if (idCliente > 0)
                {
                    var c = ClientesBLL.ReadCliente(idCliente);
                    if (c != null)
                    {
                        if (c.ImagenLogo != null)
                        {
                            logoFileName = c.ImagenLogo;
                        }
                    }
                }

                rptViewer.LocalReport.EnableExternalImages = true;
                string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                string ext = "";
                MemoryStream ms = FileHelper.GetFileStream(logoFileName, TipoArchivoEnum.ImagenLogoUsuario, out ext);
                //string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                ImagenLogo imgLogo = new ImagenLogo();
                imgLogo.Bytes = ms.ToArray();

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ventasActivasDetalleDS", ventas));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));


                if (idCliente > 0)
                {
                    rptViewer.LocalReport.SetParameters(new ReportParameter("cliente", cliente));
                    rptViewer.LocalReport.SetParameters(new ReportParameter("pathLogo", imagePath));
                    rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenLogoDS", new ImagenLogo[] { imgLogo }));
                }
                else
                {

                    if (totales.HasValue)
                        rptViewer.LocalReport.SetParameters(new ReportParameter("totales", totales.Value.ToString()));
                }
                if (autorizaciones.HasValue)
                    rptViewer.LocalReport.SetParameters(new ReportParameter("tipoConsumo",
                        "Consumos por " + (autorizaciones.Value ? "autorización telefónica" : "terminal punto de venta")));


                rptViewer.LocalReport.Refresh();
            }
        }

        /// <summary>
        /// Resetea el control a sus valores por default
        /// </summary>
        public void Reset()
        {
            rptViewer.Reset();
        }

        public void LoadClientesReport(DateTime? fechaInicial = null, DateTime? fechaFinal = null, List<int> idsTipo = null, bool? activo = null)
        {
            var clientes = ClientesBLL.GetClientesAsesorTipo(fechaInicial, fechaFinal, idsTipo, activo);
            if (HayResultados(clientes.Count))
            {
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/ClientesRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("clientesDS", clientes));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadTransBancariasClientesReport(DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idCliente = 0, List<int> idsEstatus = null, ModoReporteTransferencia modo = ModoReporteTransferencia.SoloTransferencias)
        {
            IEnumerable<object> trans = null;
            string reportPath;
            if (modo == ModoReporteTransferencia.PagosAplicados)
            {
                trans = TransBancariasBLL.GetTransBancariasClientesDetalle(fechaInicial, fechaFinal, idCliente, idsEstatus);
                reportPath = "~/Reports/TransBancariasClientesDetalleRpt.rdlc";
            }
            else
            {
                trans = TransBancariasBLL.GetTransBancariasClientes(fechaInicial, fechaFinal, idCliente, idsEstatus);
                reportPath = "~/Reports/TransBancariasClientesRpt.rdlc";
            }
            if (HayResultados(trans.Count()))
            {
                using (FileStream fs = File.OpenRead(MapPath(reportPath)))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("transBancariasDS", trans));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                if (modo != ModoReporteTransferencia.PagosAplicados)
                    rptViewer.LocalReport.SetParameters(new ReportParameter("soloTotales", (modo == ModoReporteTransferencia.TotalCliente).ToString()));
                rptViewer.LocalReport.Refresh();

            }
        }

        public void LoadLineasCreditoReport(DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idCliente = 0)
        {
            List<AjustesLineaCredito> trans = CuentasBLL.GetAjustesLineaCredito(fechaInicial, fechaFinal, idCliente);
            if (HayResultados(trans.Count))
            {
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/LineaCreditoRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ajustesLineaCreditoDS", trans));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadTransferenciasConsumosReport(int idUsuario, DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idCliente = 0,
            List<int> idsTarjetas = null, List<int> idsCentrosCostos = null, int idasesor = 0)
        {
            List<int> idCltes = null;
            if (idasesor > 0)
                idCltes = ClientesBLL.GetClientes().Where(w => w.IdAsesor == idasesor).Select(s => s.IdCliente).ToList();
            var trans = TarjetasBLL.GetTransferenciasConsumos(fechaInicial, fechaFinal, idCliente, idsTarjetas, idsCentrosCostos, idsClientes: idCltes);
            string logoFileName = "default.png";


            if (HayResultados(trans.Count))
            {
                var c = ClientesBLL.GetClienteByUsuario(idUsuario);

                if (c != null)
                {
                    if (c.ImagenLogo != null)
                    {
                        logoFileName = c.ImagenLogo;
                    }
                }

                using (FileStream fs = File.OpenRead(MapPath("~/Reports/TransferenciasConsumosRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }

                rptViewer.LocalReport.EnableExternalImages = true;
                string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                string ext = "";
                MemoryStream ms = FileHelper.GetFileStream(logoFileName, TipoArchivoEnum.ImagenLogoUsuario, out ext);
                //string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                ImagenLogo imgLogo = new ImagenLogo();
                imgLogo.Bytes = ms.ToArray();

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("transferenciasConsumosDS", trans));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("pathLogo", imagePath));
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenLogoDS", new ImagenLogo[] { imgLogo }));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadEstacionesReport(bool soloActivas, bool soloGrupo, DateTime? fechaInicial = null, DateTime? fechaFinal = null)
        {

            var estaciones = EstacionesBLL.GetEstaciones(soloActivas, soloGrupo, fechaInicial, fechaFinal);
            if (HayResultados(estaciones.Count))
            {
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/EstacionesRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("EstacionesDS", estaciones));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadComisionesReport(DateTime? fechaInicial = null, DateTime? fechaFinal = null, List<int> idsClientes = null, int idEstatus = 0, bool soloTotales = false)
        {
            var comisiones = ReportesBLL.GetComisiones(fechaInicial, fechaFinal, idsClientes, idEstatus);
            if (HayResultados(comisiones.Count))
            {
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/ComisionesRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("comisionesDS", comisiones));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("soloTotales", soloTotales.ToString()));
                rptViewer.LocalReport.Refresh();
            }

        }

        public void LoadEstatusFoliosReport(FolioEnum serie, DateTime fechaInicial, DateTime fechaFinal)
        {
            var folios = ReportesBLL.GetEstatusFolios(serie, fechaInicial, fechaFinal);
            if (HayResultados(folios.Count))
            {
                var timbrados = folios.Where(f => !string.IsNullOrEmpty(f.UUID));

                foreach (var item in timbrados)
                {
                    string codigo, status;
                    Helpers.ReachCoreHelper.ConsultaEstatusSAT(item.IdCFDI.Value, out codigo, out status);
                    item.EstatusSAT = status;
                }
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/FoliosEstatusRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("estatusFoliosDS", folios));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                rptViewer.LocalReport.Refresh();
            }

        }

        public void LoadCancelacionesReport(DateTime? fechaInicial = null, DateTime? fechaFinal = null, int idCliente = 0, int idEstacion = 0, int idUsuario = -1, string folioMovimiento = null)
        {
            var cancelaciones = CancelacionesBLL.GetCancelaciones(fechaInicial, fechaFinal, idCliente, idEstacion, idUsuario, folioMovimiento);
            if (HayResultados(cancelaciones.Count))
            {
                using (FileStream fs = File.OpenRead(MapPath("~/Reports/CancelacionesRpt.rdlc")))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("vistaCancelacionesDS", cancelaciones));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", fechaInicial.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFinal", fechaFinal.ToString()));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadMovimientosClienteReport(DateTime fechaIni, DateTime fechaFin, int idCliente)
        {
            var abonosCliente = MovimientosBLL.GetMovimientosCliente(fechaIni, fechaFin, idCliente);
            if (HayResultados(abonosCliente.Count))
            {

                var cliente = ClientesBLL.ReadCliente(idCliente).Cliente;
                var reportPath = "~/Reports/MovimientosRpt.rdlc";
                using (FileStream fs = File.OpenRead(MapPath(reportPath)))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }


                string logoFileName = "default.png";

                if (idCliente > 0)
                {
                    var c = ClientesBLL.ReadCliente(idCliente);
                    if (c != null)
                    {
                        if (c.ImagenLogo != null)
                        {
                            logoFileName = c.ImagenLogo;
                        }
                    }
                }

                rptViewer.LocalReport.EnableExternalImages = true;
                string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                string ext = "";
                MemoryStream ms = FileHelper.GetFileStream(logoFileName, TipoArchivoEnum.ImagenLogoUsuario, out ext);
                //string imagePath = new Uri(Server.MapPath("~/Temporal/LogosUsuarios/" + logoFileName)).AbsoluteUri;
                ImagenLogo imgLogo = new ImagenLogo();
                imgLogo.Bytes = ms.ToArray();

                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("AbonosDS", abonosCliente));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicio", fechaIni.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaFin", fechaFin.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("cliente", cliente));
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("ImagenLogoDS", new ImagenLogo[] { imgLogo }));
                rptViewer.LocalReport.Refresh();
            }
        }

        public void LoadOPGerenteVentas(OrdenPagoGV opGV)
        {
            if (opGV.lOPS.Count > 0)
            {
                var reportPath = "~/Reports/OrdenPagoGerenteVentas.rdlc";
                using (FileStream fs = File.OpenRead(MapPath(reportPath)))
                {
                    rptViewer.LocalReport.LoadReportDefinition(fs);
                }
                rptViewer.LocalReport.DataSources.Clear();
                rptViewer.LocalReport.DataSources.Add(new ReportDataSource("dsOrdenesAsesores", opGV.lOPS));
                rptViewer.LocalReport.SetParameters(new ReportParameter("gerente", opGV.Gerente));
                rptViewer.LocalReport.SetParameters(new ReportParameter("fechaInicial", opGV.fechaLimite.ToShortDateString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("pctComision", opGV.PctComision.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("totalComisionGV", opGV.TotalPagoOP.ToString()));
                rptViewer.LocalReport.SetParameters(new ReportParameter("totalOPAsesores", opGV.TotalOPAsesores.ToString()));
                rptViewer.LocalReport.Refresh();
            }
        }

        /// <summary>
        /// Determina si se debe mostrar o no el mensaje de que no hay resultados.
        /// </summary>
        /// <param name="cuenta"></param>
        /// <returns></returns>
        private bool HayResultados(int cuenta)
        {
            bool rval = cuenta > 0;
            pnlNoResult.Visible = cuenta <= 0;
            rptViewer.Visible = cuenta > 0;
            return rval;
        }
        #endregion
    }
}