using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Magikus {
    public class graph : MonoBehaviour
    {
        public enum GRAPHS { Ft, Xt, X }

        public Material material;
        public float lineWidth = 0.1f;
        public Transform curveContainer;
        public Transform asintotasContainer;

        public TMP_InputField inputField;

        public smartSlider aSlider;
        public smartSlider bSlider;
        public smartSlider cSlider;
        public smartSlider dSlider;

        public smartSlider stepSlider;
        public smartSlider trajectoriesSlider;
        public Toggle eulerMejorado;
        public TMP_Text ecuacion;

        public Slider scaleXSlider;
        public Slider scaleYSlider;
        public Slider offsetXSlider;
        public Slider offsetYSlider;

        public GameObject asintotaLabel;
        
        private string expresion = "";
        private float d = 1;
        private float c = 1;
        private float b = 1;
        private float a = 1;
        private int steps = 40;
        private int trajectories = 5;

        private bool mouseDown = false;
        private Vector2 mouseOffset;
        private Vector2 mousePos;

        private GRAPHS currentGraph = GRAPHS.Ft;
        private Vector2 bounds;
        private Vector2 offset;
        private Vector2 scale;
        private List<LineRenderer> curvas;
        private List<Asintota> asintotas;
        private List<float> raices;
        private IEcuacion ecuacionActual;

        private void Start()
        {
            curvas = new List<LineRenderer>();
            asintotas = new List<Asintota>();
            bounds = transform.localScale / 2;
            offset = new Vector2(0, 0);
            scale = new Vector2(1, 1);
            asintotaLabel.SetActive(false);
            //ecuacionActual = new Polinomica(a, b, c, d);
            ecuacionActual = new Ecuacion(a, b, c, d);
            RePlot();
        }
        private void Update()
        {
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            MouseOffset();
            MouseScale();
        }

        public void PlotFvsT() { currentGraph = GRAPHS.Ft; RePlot(); }
        public void PlotXvsT() { currentGraph = GRAPHS.Xt; RePlot(); }
        public void PlotX() { currentGraph = GRAPHS.X; RePlot(); }
        public void SetEpresion()
        {
            expresion = inputField.text;
            ecuacionActual.SetExpression(expresion);
            RePlot();
        }
        public void ResetGraph()
        {
            scaleXSlider.value = 1f;
            scaleYSlider.value = 1f;
            offsetXSlider.value = 0f;
            offsetYSlider.value = 0f;
            RePlot();
        }
        public void MouseOffset()
        {
            if (Mathf.Abs(mousePos.x) <= bounds.x && Mathf.Abs(mousePos.y) <= bounds.x && Input.GetMouseButtonDown(0))
            {
                if (!mouseDown)
                {
                    mouseOffset = mousePos - offset;
                    mouseDown = true;
                }
            }

            if (Input.GetMouseButton(0) && mouseDown)
            {
                offsetXSlider.value = (mousePos - mouseOffset).x;
                offsetYSlider.value = (mousePos - mouseOffset).y;
            }
            else { mouseDown = false; }
        }
        public void MouseScale()
        {
            if (Mathf.Abs(mousePos.x) <= bounds.x && Mathf.Abs(mousePos.y) <= bounds.x)
            {
                scaleXSlider.value += Input.mouseScrollDelta.y * 0.1f;
                scaleYSlider.value += Input.mouseScrollDelta.y * 0.1f;
            }
        }

        public void RePlot()
        {
            UpdateValues();
            UpdateLabels();

            if (curvas.Count != 0) { DestroyCurvas(); }
            if (asintotas.Count != 0) { DestroyAsintotas(); }

            PlotGraph();
            PlotCurves((x) => { return ecuacionActual.Evaluar(x); });
            PlotRaices();
        }

        private void UpdateValues()
        {
            d = (float)Math.Round(dSlider.GetValue(), 2);
            c = (float)Math.Round(cSlider.GetValue(), 2);
            b = (float)Math.Round(bSlider.GetValue(), 2);
            a = (float)Math.Round(aSlider.GetValue(), 2);

            offset = new Vector2((float)Math.Round(offsetXSlider.value, 2), (float)Math.Round(offsetYSlider.value, 2));
            scale = new Vector2((float)Math.Round(scaleXSlider.value, 2), (float)Math.Round(scaleYSlider.value, 2));

            steps = (int)stepSlider.GetValue();
            trajectories = (int)trajectoriesSlider.GetValue();
            
            ecuacionActual.SetParametros(a, b, c, d);
            raices = ecuacionActual.CalcularRaices(0.01f, -bounds.x * 2, bounds.x * 2, 0.02f);
        }
        private void UpdateLabels()
        {
            scaleYSlider.GetComponentInChildren<TMP_Text>().text = $"Escala V: {scale.y.ToString("F1")}";
            scaleXSlider.GetComponentInChildren<TMP_Text>().text = $"Escala H: {scale.x.ToString("F1")}";
            offsetYSlider.GetComponentInChildren<TMP_Text>().text = $"Offset V: {offset.y.ToString("F1")}";
            offsetXSlider.GetComponentInChildren<TMP_Text>().text = $"Offset H: {offset.x.ToString("F1")}";
            ecuacion.text = ecuacionActual.ToLabel();
        }
        private void DestroyCurvas()
        {
            for (int i = 0; i < curvas.Count; i++) { Destroy(curvas[i].gameObject); }
            curvas.Clear();
        }
        private void DestroyAsintotas()
        {
            for (int i = 0; i < asintotas.Count; i++) { asintotas[i].Dump(); }
            asintotas.Clear();
        }

        private void PlotGraph()
        {
            string xLabel = "x";
            string yLabel = "y";
            switch (currentGraph)
            {
                case GRAPHS.Ft: xLabel = "t"; yLabel = "F(x)"; break;
                case GRAPHS.Xt: xLabel = "t"; yLabel = "X(t)"; ; break;
                case GRAPHS.X: xLabel = "X"; yLabel = "0"; ; break;
            }

            int n = 40;
            float factor = (scale.x >= 2) ? 0.5f : (scale.x <= 0.5f) ? 4 : 1;
            for (float i = -n; i <= n; i += factor)
            {
                if (i == 0) { continue; }
                PlotLine_Vertical(i, Color.grey, lineWidth / 2);
            }
            factor = (scale.y >= 2) ? 0.5f : (scale.y <= 0.5f) ? 4 : 1;
            for (float i = -n; i <= n; i += factor)
            {
                if (i == 0) { continue; }
                if (currentGraph == GRAPHS.X) { continue; }
                PlotLine_Horizontal(i, Color.grey, lineWidth / 2);
            }

            PlotLine_Vertical(0f, 3.5f, yLabel, Color.white, lineWidth * 1.02f);
            PlotLine_Horizontal(0f, 3.5f, xLabel, Color.white, lineWidth * 1.02f);
        }
        private void PlotRaices()
        {
            for (int i = 0; i < raices.Count; i++)
            {
                switch (currentGraph)
                {
                    case GRAPHS.Ft: PlotLine_Horizontal(raices[i], 2.5f - (i * 1f), "", Color.blue); break;
                    case GRAPHS.Xt: PlotLine_Vertical(raices[i], -2.25f - (i * 0.5f), "", Color.blue); break;
                    case GRAPHS.X: PlotLine_Vertical(raices[i], -2.25f - (i * 0.5f), "", Color.blue); break;
                }
            }
        }
        private void PlotCurves(Func<float, float> function)
        {
            switch (currentGraph)
            {
                case GRAPHS.Ft:
                    {
                        int width = 8;
                        float h = 0.05f * (21 - trajectories);
                        for (float x0 = -width; x0 < width; x0 += h)
                        {
                            if (eulerMejorado.isOn)
                            {
                                PlotCurve(FitToGraph(EulerMejorado(steps, x0, function, 0))).SetColor(Color.red, Color.green).SetLineWidth(lineWidth, lineWidth / 2);
                            }
                            else
                            {
                                PlotCurve(FitToGraph(Euler(steps, x0, function, 0))).SetColor(Color.red, Color.green).SetLineWidth(lineWidth, lineWidth / 2);
                            }
                        }
                        break;
                    }
                case GRAPHS.Xt: PlotCurve(FitToGraph(CalculateCurve(steps, function))).SetColor(Color.green); break;
                case GRAPHS.X:
                    {
                        Color startColor;
                        Color endColor;
                        float startWidth;
                        float endWidth;
                        float midpoint;

                        if (raices.Count == 0) { return; }

                        midpoint = (-bounds.x + raices[0]) / 2;
                        if (ecuacionActual.Evaluar(midpoint) > 0) { startColor = Color.red; endColor = Color.green; startWidth = lineWidth * 8; endWidth = lineWidth; } 
                        else { startColor = Color.green; endColor = Color.red; startWidth = lineWidth; endWidth = lineWidth * 8; }
                        PlotLine_X(0, new Vector2(-bounds.x, raices[0]), new Vector2(midpoint, 1f), $"(-inf, {raices[0].ToString("F1")})", startColor, endColor, startWidth, endWidth);

                        int i;
                        if (raices.Count != 1)
                        {
                            for (i = 0; i < raices.Count - 1; i++)
                            {
                                midpoint = (raices[i] + raices[i + 1]) / 2;
                                if (ecuacionActual.Evaluar(midpoint) > 0) { startColor = Color.red; endColor = Color.green; startWidth = lineWidth * 8; endWidth = lineWidth; }
                                else { startColor = Color.green; endColor = Color.red; startWidth = lineWidth; endWidth = lineWidth * 8; }
                                PlotLine_X(0, new Vector2(raices[i], raices[i + 1]), new Vector2(midpoint, 1f * ((i % 2 == 0) ? -1 : 1)), $"({raices[i].ToString("F1")}, {raices[i + 1].ToString("F1")})", startColor, endColor, startWidth, endWidth);
                            }
                        }
                        i = raices.Count - 1;
                        midpoint = (bounds.x + raices[i]) / 2;
                        if (ecuacionActual.Evaluar(midpoint) > 0) { startColor = Color.red; endColor = Color.green; startWidth = lineWidth * 8; endWidth = lineWidth; }
                        else { startColor = Color.green; endColor = Color.red; startWidth = lineWidth; endWidth = lineWidth * 8; }
                        PlotLine_X(0, new Vector2(raices[i], bounds.x), new Vector2(midpoint, 1f * ((i % 2 == 0) ? -1 : 1)), $"({raices[i].ToString("F1")}, inf)", startColor, endColor, startWidth, endWidth);
                        
                        break;
                    } 
            }
        }

        private LineRenderer PlotCurve(List<Vector3> coord)
        {
            GameObject line = new GameObject($"Line - {curvas.Count}", typeof(LineRenderer));
            line.transform.parent = curveContainer;
            line.transform.position = Vector3.zero;

            LineRenderer renderer = line.GetComponent<LineRenderer>();
            curvas.Add(renderer);
            renderer.material = material;
            renderer.startWidth = lineWidth;
            renderer.endWidth = lineWidth;

            renderer.positionCount = coord.Count;
            for (int i = 0; i < coord.Count; i++) { coord[i] = new Vector3(coord[i].x + transform.position.x, coord[i].y + transform.position.y); }
            renderer.SetPositions(coord.ToArray());

            return renderer;
        }
        private void PlotLine_Vertical(float x, float labelPos = 0, string labelText = "", Color color = default, float lineWidth = 0)
        {
            if (!HorizontalFit(x)) { return; }

            if (color == default) { color = Color.white; }
            if (lineWidth == 0) { lineWidth = this.lineWidth; }

            asintotas.Add(new Asintota(x, Instantiate(asintotaLabel, asintotasContainer), FitToGraph_VerticalFix(x, labelPos), labelText));
            PlotCurve(new List<Vector3>() { FitToGraph_VerticalFix(x, -bounds.y), FitToGraph_VerticalFix(x, bounds.y) }).SetColor(color).SetLineWidth(lineWidth);
        }
        private void PlotLine_Horizontal(float y, float labelPos = 0, string labelText = "", Color color = default, float lineWidth = 0)
        {
            if (!VerticalFit(y)) { return; }

            if (color == default) { color = Color.white; }
            if (lineWidth == 0) { lineWidth = this.lineWidth; }

            asintotas.Add(new Asintota(y, Instantiate(asintotaLabel, asintotasContainer), FitToGraph_HorizontalFix(labelPos, y), labelText));
            PlotCurve(new List<Vector3>() { FitToGraph_HorizontalFix(-bounds.x, y), FitToGraph_HorizontalFix(bounds.x, y) }).SetColor(color).SetLineWidth(lineWidth);
        }
        private void PlotLine_X(float y, Vector2 intervalo, Vector2 labelPos, string labelText, Color startColor, Color endColor, float startWidth = 0, float endWidth = 0)
        {
            if (!VerticalFit(0)) { return; }

            if (startWidth == 0) { startWidth = this.lineWidth; }
            if (endWidth == 0) { endWidth = this.lineWidth; }

            asintotas.Add(new Asintota(0, Instantiate(asintotaLabel, asintotasContainer), FitToGraph_HorizontalFix(labelPos.x, labelPos.y), labelText));
            PlotCurve(new List<Vector3>() { FitToGraph_HorizontalFix(intervalo.x, y), FitToGraph_HorizontalFix(intervalo.y, y) }).SetColor(startColor, endColor).SetLineWidth(startWidth, endWidth);
        }
        private void PlotLine_Vertical(float x, Color color = default, float lineWidth = 0)
        {
            if (!HorizontalFit(x)) { return; }

            if (color == default) { color = Color.white; }
            if (lineWidth == 0) { lineWidth = this.lineWidth; }

            PlotCurve(new List<Vector3>() { FitToGraph_VerticalFix(x, -bounds.y), FitToGraph_VerticalFix(x, bounds.y) }).SetColor(color).SetLineWidth(lineWidth);
        }
        private void PlotLine_Horizontal(float y, Color color = default, float lineWidth = 0)
        {
            if (!VerticalFit(y)) { return; }

            if (color == default) { color = Color.white; }
            if (lineWidth == 0) { lineWidth = this.lineWidth; }

            PlotCurve(new List<Vector3>() { FitToGraph_HorizontalFix(-bounds.x, y), FitToGraph_HorizontalFix(bounds.x, y) }).SetColor(color).SetLineWidth(lineWidth);
        }

        private List<Vector3> FitToGraph(List<Vector3> curve) { for (int i = 0; i < curve.Count; i++) { curve[i] = FitToGraph(curve[i].x, curve[i].y); } return curve; }
        private Vector3 FitToGraph(float x, float y) { return new Vector3(Mathf.Clamp((x + offset.x) * scale.x, -bounds.x - 0.2f, bounds.x + 0.2f), Mathf.Clamp((y + offset.y) * scale.y, -bounds.y - 0.2f, bounds.y + 0.2f)); }
        private Vector3 FitToGraph_HorizontalFix(float x, float y) { return new Vector3(Mathf.Clamp(x, -bounds.x - 0.2f, bounds.x + 0.2f), Mathf.Clamp((y + offset.y) * scale.y, -bounds.y - 0.2f, bounds.y + 0.2f)); }
        private Vector3 FitToGraph_VerticalFix(float x, float y) { return new Vector3(Mathf.Clamp((x + offset.x) * scale.x, -bounds.x - 0.2f, bounds.x + 0.2f), Mathf.Clamp(y, -bounds.y - 0.2f, bounds.y + 0.2f)); }

        private bool VerticalFit(float y) { return (((y + offset.y) * scale.y) >= -bounds.y) && (((y + offset.y) * scale.y) <= bounds.y); }
        private bool HorizontalFit(float x) { return (((x + offset.x) * scale.x) >= -bounds.x) && (((x + offset.x) * scale.x) <= bounds.x); }

        private List<Vector3> CalculateCurve(int n, Func<float, float> ecuacion)
        {
            List<Vector3> coord = new List<Vector3>();

            float x;
            float y;
            float h = (bounds.x * 4) / n;

            for (int i = 0; i <= n; i++)
            {
                x = -(bounds.x * 2) + (i * h);
                y = ecuacion.Invoke(x);
                if (float.IsNaN(x) || float.IsNaN(y)) { continue; }
                coord.Add(x, y);
            }
            return coord;
        }
        private List<Vector3> Euler(int n, float f0, Func<float, float> ecuacion, float timeOffset = 0)
        {
            List<Vector3> coord = new List<Vector3>();

            float x = -bounds.x + timeOffset;
            float f = f0;
            float h = (bounds.x * 2) / n;

            coord.Add(x, f0);

            for (int i = 1; i <= n; i++)
            {
                x = -bounds.x + timeOffset + (i * h);
                f = f + (h * ecuacion.Invoke(f));

                if (float.IsNaN(f) || float.IsNaN(x)) { continue; }
                coord.Add(x, f);
            }
            return coord;
        }
        private List<Vector3> EulerMejorado(int n, float f0, Func<float, float> ecuacion, float timeOffset = 0)
        {
            List<Vector3> coord = new List<Vector3>();

            float x = -bounds.x + timeOffset;
            float f = f0;
            float h = (bounds.x * 2) / n;
            float aux;

            coord.Add(x, f0);

            for (int i = 1; i <= n; i++)
            {
                x = -bounds.x + timeOffset + (i * h);
                aux = f + (h * ecuacion.Invoke(f));
                f = f + (0.5f * h * (ecuacion.Invoke(f) + ecuacion.Invoke(aux)));

                if (float.IsNaN(f) || float.IsNaN(x)) { continue; }
                coord.Add(x, f);
            }
            return coord;
        }

        

        public class Asintota
        {
            public float valor;
            public string labelText;
            private GameObject label;

            public Asintota(float valor, GameObject label, Vector3 position, string labelText = "")
            {
                this.valor = valor;
                this.label = label;
                this.labelText = labelText;
                SetLabel(position);
            }

            public void SetLabel(Vector3 position)
            {
                if (label == null) { return; }
                label.GetComponent<TextMeshPro>().text = (labelText == "") ? valor.ToString("F1") : labelText;
                label.transform.position = position;
                label.SetActive(true);
            }
            public void Dump() { Destroy(label); }
        }

        public interface IEcuacion
        {
            string ToLabel();
            void SetParametros(float a, float b, float c, float d);
            void SetExpression(string expresion);

            List<float> CalcularRaices();
            List<float> CalcularRaices(int n, float x0, float xn, float sensibilidad = 0.01f);
            List<float> CalcularRaices(float h, float x0, float xn, float sensibilidad = 0.01f);
            float Evaluar(float x);
        }
        public class Ecuacion : IEcuacion
        {
            public string ecuacion = "";
            public string prefix = "";
            public float a;
            public float b;
            public float c;
            public float d;
            Dictionary<char, float> dictionary;

            public Ecuacion(float a, float b, float c, float d, string ecuacion = "")
            {
                this.ecuacion = ecuacion;
                dictionary = new Dictionary<char, float>();
                dictionary.Add('a', a);
                dictionary.Add('b', b);
                dictionary.Add('c', c);
                dictionary.Add('d', d);
                SetParametros(a, b, c, d);
                SetExpression(ecuacion);
            }
            public virtual float Evaluar(float x)
            {
                if (!dictionary.ContainsKey('x')) { dictionary.Add('x', x); } else { dictionary['x'] = x; }
                return MathExp.PostfixEvaluate(prefix, dictionary);
            }

            public virtual List<float> CalcularRaices() { return new List<float>(); }
            public virtual List<float> CalcularRaices(int n, float x0, float xn, float sensibilidad = 0.01f)
            {
                List<float> raices = new List<float>();

                float x;
                float y;
                float h = (xn - x0) / n;

                for (int i = 0; i <= n; i++)
                {
                    x = x0 + h * i;
                    if (float.IsNaN(x)) { continue; }
                    y = (float)Math.Round(Evaluar(x), 1);
                    if (float.IsNaN(y)) { continue; }

                    if (Mathf.Abs(y) <= sensibilidad) { raices.Add(x); }
                }

                return LimpiarRaices(raices, 0.3f);
            }
            public virtual List<float> CalcularRaices(float h, float x0, float xn, float sensibilidad = 0.01f)
            {
                List<float> raices = new List<float>();

                float x;
                float y;
                float n = (xn - x0) / h;

                for (int i = 0; i <= n; i++)
                {
                    x = x0 + h * i;
                    if (float.IsNaN(x)) { continue; }
                    y = (float)Math.Round(Evaluar(x), 1);
                    if (float.IsNaN(y)) { continue; }

                    if (Mathf.Abs(y) <= sensibilidad) { raices.Add(x); }
                }

                return LimpiarRaices(raices, 0.3f);
            }
            public virtual void SetExpression(string expresion)
            {
                if (expresion != "")
                {
                    ecuacion = expresion;
                    prefix = MathExp.InfixToPostfix(expresion);
                }
            }
            public virtual void SetParametros(float a, float b, float c, float d)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                this.d = d;
                dictionary['a'] = a;
                dictionary['b'] = b;
                dictionary['c'] = c;
                dictionary['d'] = d;
            }
            public virtual string ToLabel()
            {
                string exp = MathExp.PrepareExpression(ecuacion);
                exp = MathExp.ReplaceParameter(exp, "a", a);
                exp = MathExp.ReplaceParameter(exp, "b", b);
                exp = MathExp.ReplaceParameter(exp, "c", c);
                exp = MathExp.ReplaceParameter(exp, "d", d);
                exp = MathExp.RestoreExpression(exp);
                exp = exp.Replace("*x*x*x*", "(x^4)");
                exp = exp.Replace("*x*x*x", "(x^3)");
                exp = exp.Replace("*x*x", "(x^2)");
                return $"f(x) = {exp}"; 
            }

            private List<float> LimpiarRaices(List<float> raices, float sensibilidad)
            {
                if (raices.Count < 2) { return raices; }
                for (int i = 0; i < raices.Count; i++)
                {
                    for (int j = raices.Count - 1; j > i; j--)
                    {
                        if (Mathf.Abs(raices[i] - raices[j]) <= sensibilidad) { raices.RemoveAt(j); }
                    }
                }
                return raices;
            }
        }
        public class Polinomica : Ecuacion
        {
            public Polinomica(float a, float b, float c, float d) : base(a, b, c, d) { }
            public override float Evaluar(float x) { return (d * x * x * x) + (c * x * x) + (b * x) + a; }
            public override List<float> CalcularRaices()
            {
                List<float> raices = new List<float>();

                if (b * b < 4 * c * a) { return raices; }

                if (c != 0 && b * b != 4 * c * a)
                {
                    raices.Add((-b - Mathf.Pow(Mathf.Pow(b, 2) - (4 * c * a), 0.5f)) / (2 * c));
                    raices.Add((-b + Mathf.Pow(Mathf.Pow(b, 2) - (4 * c * a), 0.5f)) / (2 * c));
                    return raices;
                }
                if (b * b == 4 * c * a) { raices.Add(-b / c * 0.5f); return raices; }
                if (b != 0) { raices.Add(-a / b); return raices; }
                return raices;
            }
            public override string ToLabel() { return "f(x) =" + ((d == 0) ? "" : $"{d.ToString(" + 0.0; - 0.0")}.(x^3)") + ((c == 0) ? "" : $"{c.ToString(" + 0.0; - 0.0")}.(x^2)") + ((b == 0) ? "" : $" {b.ToString(" + 0.0; - 0.0")}.(x)") + ((a == 0) ? "" : $" {a.ToString(" + 0.0; - 0.0")}"); }
        }
        public class Senoidal : Ecuacion
        {
            public Senoidal(float a, float b, float c, float d) : base(a, b, c, d) { }
            public override float Evaluar(float x) { return a * Mathf.Sin((x * b) + c); }
            public override string ToLabel() { return $"f(x) ={a.ToString("  0.0; -0.0")}.Sen({b.ToString("0.0;-0.0")}.x + {c.ToString(" + 0.0; - 0.0")})"; }
        }
        public class Exponencial : Ecuacion
        {
            public Exponencial(float a, float b, float c, float d) : base(a, b, c, d) { }
            public override float Evaluar(float x) { return a * Mathf.Exp((x * b) + c); }
            public override string ToLabel() { return $"f(x) ={a.ToString("  0.0; -0.0")}.e^({b.ToString("0.0;-0.0")}.x + {c.ToString(" + 0.0; - 0.0")})"; }
        }
    }

    public static class MathExp // d*x*x*x+c*x*x+b*x+a
    {
        public static float PostfixEvaluate(string exp, Dictionary<char, float> dictionary)
        {
            if (exp == "") { return float.NaN; }
            if (exp == "Invalid Expresion") { return float.NaN; }

            Stack<float> values = new Stack<float>();

            for (int i = 0; i < exp.Length; i++)
            {
                if (!char.IsLetterOrDigit(exp[i])) { EvaluateOperation(exp[i], values); }
                else { values.Push(dictionary[exp[i]]); }
            }

            return values.Pop();
        }
        public static float InfixEvaluate(string exp, Dictionary<char, float> dictionary)
        {
            if (exp == "") { return float.NaN; }

            Stack<float> values = new Stack<float>();

            //MonoBehaviour.print(exp);

            exp = InfixToPostfix(exp);
            if (exp == "Invalid Expresion") { return float.NaN; }

            //MonoBehaviour.print(exp);

            for (int i = 0; i < exp.Length; i++)
            {
                if (!char.IsLetterOrDigit(exp[i])) { EvaluateOperation(exp[i], values); }
                else { values.Push(dictionary[exp[i]]); }
            }

            return values.Pop();
        }
        public static float Evaluate(string exp)
        {
            if (exp == "") { return float.NaN; }
            
            Dictionary<char,float> dictionary = new Dictionary<char, float>();
            Stack<float> values = new Stack<float>();

            //MonoBehaviour.print(exp);

            exp = ExtractNumbers(exp, dictionary);
            if(exp == "Too Many Arguments!") { return float.NaN; }

            //MonoBehaviour.print(exp);
            //MonoBehaviour.print(dictionary.Count);

            exp = InfixToPostfix(exp);
            if (exp == "Invalid Expresion") { return float.NaN; }

            //MonoBehaviour.print(exp);

            for (int i = 0; i < exp.Length; i++) 
            {
                if (!char.IsLetterOrDigit(exp[i])) { EvaluateOperation(exp[i], values); } 
                else { values.Push(dictionary[exp[i]]); }
            }

            return values.Pop();
        }
        public static string ReplaceParameter(string exp, string parameter, float value) 
        {
            return exp.Replace(parameter, ((value < 0) ? "|" : "") + Mathf.Abs(value).ToString());
        }
        public static string RestoreExpression(string exp) 
        {
            exp = exp.Replace("#", "sin");
            exp = exp.Replace("$", "cos");
            exp = exp.Replace("&", "tan");
            exp = exp.Replace("¿", "exp");
            exp = exp.Replace("?", "log");
            exp = exp.Replace("+|", "-");
            exp = exp.Replace("|", "-");
            return exp;
        }
        public static string PrepareExpression(string exp) 
        {
            exp = exp.ToLower();
            exp = exp.Replace("[", "(").Replace("]", ")");
            exp = exp.Replace("{", "(").Replace("}", ")");
            exp = exp.Replace("sin", "#");
            exp = exp.Replace("cos", "$");
            exp = exp.Replace("tan", "&");
            exp = exp.Replace("exp", "¿");
            exp = exp.Replace("log", "?");
            return exp;
        }
        public static void EvaluateOperation(char op, Stack<float> values)
        {
            switch (op)
            {
                case '+': values.Push(values.Pop() + values.Pop()); return;
                case '-': values.Push(values.Pop() - values.Pop()); return;
                case '*': values.Push(values.Pop() * values.Pop()); return;
                case '/': 
                    {
                        float a = values.Pop();
                        values.Push(values.Pop() / a); 
                        return; 
                    }
                case '^': 
                    {
                        float a = values.Pop();
                        values.Push(Mathf.Pow(values.Pop(), a)); 
                        return;
                    }

                case '!': values.Push(values.Pop()); return;
                case '%':
                    {
                        float a = values.Pop();
                        values.Push(values.Pop() % a); 
                        return;
                    }
                case '#': values.Push(Mathf.Sin(values.Pop())); return;
                case '$': values.Push(Mathf.Cos(values.Pop())); return;
                case '&': values.Push(Mathf.Tan(values.Pop())); return;

                case '¿': values.Push(Mathf.Exp(values.Pop())); return;
                case '?': values.Push(Mathf.Log(values.Pop())); return;
                case '|': values.Push(-values.Pop()); return;
            }
        }
        public static string ExtractNumbers(string exp, Dictionary<char, float> dictionary)
        {
            string result = "";
            string number = "";
            string indexes = "0123456789abcdefghijklmnopqrstupvwxyz";

            for (int i = 0; i < exp.Length; ++i)
            {
                if (dictionary.Count > indexes.Length) { return "Too Many Arguments!"; }
                char c = exp[i];

                if (char.IsDigit(c) || c == '.' || c=='e') { number += c; } // If the scanned character is an operand, add it to output.
                else
                {
                    if (number != "")
                    {
                        result += indexes[dictionary.Count];
                        dictionary.Add(indexes[dictionary.Count], float.Parse(number));
                        number = "";
                    }
                    result += c;
                }
            }
            if (number != "")
            {
                result += indexes[dictionary.Count];
                dictionary.Add(indexes[dictionary.Count], float.Parse(number));
            }

            return result;
        }
        public static string InfixToPostfix(string exp)
        {
            string result = "";
            Stack<char> stack = new Stack<char>();

            exp = PrepareExpression(exp);

            for (int i = 0; i < exp.Length; ++i)
            {
                char c = exp[i];

                if (char.IsLetterOrDigit(c)) { result += c; }
                else if (c == '(') { stack.Push(c); }
                else if (c == ')')
                {
                    while (stack.Count > 0 && stack.Peek() != '(') { result += stack.Pop(); }
                    if (stack.Count > 0 && stack.Peek() != '(') { return "Invalid Expression!"; }
                    else { stack.Pop(); }
                }
                else
                {
                    while (stack.Count > 0 && Precedence(c) <= Precedence(stack.Peek())) { result += stack.Pop(); }
                    stack.Push(c);
                }
            }
            while (stack.Count > 0) { result += stack.Pop(); }

            return result;
        }
        public static int Precedence(char ch)
        {
            switch (ch)
            {
                case '+': return 1;
                case '-': return 1;
                case '*': return 2;
                case '/': return 2;
                case '^': return 3;

                case '!': return 3;
                case '%': return 3;

                case '#': return 3;
                case '$': return 3;
                case '&': return 3;

                case '¿': return 3;
                case '?': return 3;

                case '|': return 3;
            }
            return -1;
        }
    }
    public static class Extensions 
    {
        public static void Add(this List<Vector3> vectorList, float x, float y) { vectorList.Add(new Vector3(x, y)); }
        public static LineRenderer SetColor(this LineRenderer renderer, Color color)
        {
            renderer.startColor = color;
            renderer.endColor = color;
            return renderer;
        }
        public static LineRenderer SetColor(this LineRenderer renderer, Color start, Color end)
        {
            renderer.startColor = start;
            renderer.endColor = end;
            return renderer;
        }
        public static LineRenderer SetLineWidth(this LineRenderer renderer, float lineWidth)
        {
            renderer.startWidth = lineWidth;
            renderer.endWidth = lineWidth;
            return renderer;
        }
        public static LineRenderer SetLineWidth(this LineRenderer renderer, float start, float end)
        {
            renderer.startWidth = start;
            renderer.endWidth = end;
            return renderer;
        }
    }
}
