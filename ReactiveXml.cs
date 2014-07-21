using ReactiveXml.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReactiveXml
{
    public static class ReactiveXml
    {
        #region Extensions

        /// <summary>
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns>A stream of unique results of element.Element(name)</returns>
        public static LightweightObservable<XElement> ElementAsObservable(this XElement element, XName name)
        {
            return new LightweightObservable<XElement>
            {
                EqualityComparer = EqualityComparer<XElement>.Default,
                SelectNow = true,
                Context = element,
                Selector = () => element.Element(name),
                Filter = c =>
                    {
                        if (c.Change == XObjectChange.Value)
                        {
                            return false;
                        }
                        if (c.Changed.Parent != element && c.OldParent != element)
                        {
                            return false;
                        }

                        var el = c.Changed as XElement;
                        if (el == null || (el.Name != name && c.OldName != name))
                        {
                            return false;
                        }

                        return true;
                    }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns>A stream of unique results of element.Elements(name)</returns>
        public static LightweightObservable<IEnumerable<XElement>> ElementsAsObservable(this XElement element, XName name)
        {
            return new LightweightObservable<IEnumerable<XElement>>
            {
                EqualityComparer = SequenceEqualEqualityComparer<XElement>.Instance,
                SelectNow = true,
                Context = element,
                Selector = () => element.Elements(name),
                Filter = (c) =>
                    {
                        if (c.Change == XObjectChange.Value)
                        {
                            return false;
                        }
                        if (c.Changed.Parent != element && c.OldParent != element)
                        {
                            return false;
                        }

                        var el = c.Changed as XElement;
                        if (el == null || (el.Name != name && c.OldName != name))
                        {
                            return false;
                        }

                        return true;
                    }
            };
        }

        /// <summary>
        /// Note: this is a bit akward. When XElement.SetValue is called, it removes and adds XText node - while
        /// rasing events. So observing only makes sense for nullable types, as you get obligatory null
        /// on each change :(
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns>A stream of unique results of (T)element.Attribute(name)</returns>
        public static LightweightObservable<T> ValueAsObservable<T>(this XElement element)
        {
            return new LightweightObservable<T>
            {
                EqualityComparer = EqualityComparer<T>.Default,
                SelectNow = true,
                Context = element,
                Selector = () =>
                    {
                        if (string.IsNullOrEmpty(element.Value))
                        {
                            return ExplicitCast<T>.From((XElement)null);
                        }
                        return ExplicitCast<T>.From(element);
                    },
                Filter = (c) =>
                    {
                        if (c.Changed.Parent != element && c.OldParent != element)
                        {
                            return false;
                        }
                        if (c.Changed is XText == false)
                        {
                            return false;
                        }
                        return true;
                    }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns>A stream of unique results of (T)element.Attribute(name)</returns>
        public static LightweightObservable<T> AttributeValueAsObservable<T>(this XElement element, XName name)
        {
            return new LightweightObservable<T>
            {
                EqualityComparer = EqualityComparer<T>.Default,
                SelectNow = true,
                Context = element,
                Selector = () => ExplicitCast<T>.From(element.Attribute(name)),
                Filter = (c) =>
                    {
                        if (c.Changed.Parent != element && c.OldParent != element)
                        {
                            return false;
                        }

                        var attr = c.Changed as XAttribute;
                        if (attr == null || (attr.Name != name && c.OldName != name))
                        {
                            return false;
                        }

                        return true;
                    }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="expression"></param>
        /// <param name="namespaceResolver"></param>
        /// <returns></returns>
        public static LightweightObservable<T> XPathSelectAttributeValueAsObservable<T>(this XElement element, string expression, IXmlNamespaceResolver namespaceResolver = null)
        {
            return new LightweightObservable<T>
            {
                EqualityComparer = EqualityComparer<T>.Default,
                SelectNow = true,
                Context = element,
                Selector = () =>
                    {
                        var result = (IEnumerable)element.XPathEvaluate(expression, namespaceResolver);
                        var attribute = result.Cast<XAttribute>().FirstOrDefault();
                        return ExplicitCast<T>.From(attribute);
                    }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="expression"></param>
        /// <param name="namespaceResolver"></param>
        /// <returns></returns>
        public static LightweightObservable<XElement> XPathSelectElementAsObservable(this XElement element, string expression, IXmlNamespaceResolver namespaceResolver = null)
        {
            return new LightweightObservable<XElement>
            {
                EqualityComparer = EqualityComparer<XElement>.Default,
                SelectNow = true,
                Context = element,
                Selector = () =>
                    {
                        var result = element.XPathSelectElement(expression, namespaceResolver);
                        return result;
                    }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="expression"></param>
        /// <param name="namespaceResolver"></param>
        /// <returns></returns>
        public static LightweightObservable<IEnumerable<XElement>> XPathSelectElementsAsObservable(this XElement element, string expression, IXmlNamespaceResolver namespaceResolver)
        {
            return new LightweightObservable<IEnumerable<XElement>>
            {
                EqualityComparer = SequenceEqualEqualityComparer<XElement>.Instance,
                SelectNow = true,
                Context = element,
                Selector = () =>
                    {
                        var result = element.XPathSelectElements(expression, namespaceResolver);
                        return result;
                    },
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static LightweightObservable<XElementChange> ChangesAsObservable(this XElement element)
        {
            XElementChange c = new XElementChange { Context = element };
            return new LightweightObservable<XElementChange>
            {
                EqualityComparer = null,
                SelectNow = false,
                Context = element,
                Selector = () => c,
                Filter = (cc) =>
                    {
                        c = cc;
                        return true;
                    }
            };
        }

        #endregion

        #region Types

        /// <summary>
        /// A struct that passes delegates to any subscription created. A struct, so won't need an allocation (unless boxed...).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public struct LightweightObservable<T> : IObservable<T>
        {
            internal XElement Context { get; set; }
            internal Selector<T> Selector { get; set; }
            internal Filter Filter { get; set; }
            internal IEqualityComparer<T> EqualityComparer { get; set; }
            internal bool SelectNow { get; set; }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return new Subscription<T>(observer, Context, Selector, Filter, EqualityComparer, SelectNow);
            }
        }

        #endregion
    }

    /// <summary>
    /// Provides more programmer-friendly XElement changes tracking. Basically, merges OnChanging and OnChanged into one.
    /// </summary>
    public struct XElementChange
    {
        /// <summary>
        /// An element for which the event was registerd.
        /// </summary>
        public XElement Context { get; internal set; }
        /// <summary>
        /// Thing that has changed.
        /// </summary>
        public XObject Changed { get; internal set; }
        /// <summary>
        /// Previous parent of the thing that has changed. Useful with remove.
        /// </summary>
        public XElement OldParent { get; internal set; }
        /// <summary>
        /// Previous value.
        /// </summary>
        public string OldValue { get; internal set; }
        /// <summary>
        /// 
        /// </summary>
        public XName OldName { get; internal set; }
        /// <summary>
        /// What's the operation exactly?
        /// </summary>
        public XObjectChange Change { get; internal set; }
    }

    // you may want to move it to a separate file...
    namespace Internal
    {
        internal delegate T Selector<T>();
        internal delegate bool Filter(XElementChange change);

        internal class Subscription<T> : IDisposable
        {
            private IObserver<T> m_observer;
            private T m_lastResult;
            private IEqualityComparer<T> m_comparer;
            private XElement m_context;
            private Selector<T> m_selector;
            private Filter m_filter;
            private XElementChange m_change;

            public Subscription(IObserver<T> observer, XElement context, Selector<T> selector, Filter filter, IEqualityComparer<T> comparer, bool selectNow)
            {
                m_observer = observer;
                m_context = context;
                m_selector = selector;
                m_filter = filter;
                m_comparer = comparer;

                m_context.Changing += OnContextChanging;
                m_context.Changed += OnContextChanged;

                if (selectNow)
                {
                    try
                    {
                        m_lastResult = m_selector();
                    }
                    catch (Exception ex)
                    {
                        m_observer.OnError(ex);
                        return;
                    }

                    // on success, do next
                    m_observer.OnNext(m_lastResult);
                }
            }

            public void Dispose()
            {
                m_context.Changed -= OnContextChanged;
                m_context.Changing -= OnContextChanging;
            }

            private void OnContextChanging(object sender, XObjectChangeEventArgs e)
            {
                if (m_filter == null)
                {
                    // nothing to do here
                    return;
                }

                m_change.Change = e.ObjectChange;
                m_change.Changed = (XObject)sender;
                m_change.Context = m_context;
                m_change.OldParent = m_change.Changed.Parent;

                if (sender is XAttribute)
                {
                    var attr = (XAttribute)sender;
                    m_change.OldName = attr.Name;
                    m_change.OldValue = attr.Value;
                }
                else if (sender is XElement)
                {
                    var el = (XElement)sender;
                    m_change.OldName = el.Name;
                    m_change.OldValue = el.Value;
                }
            }

            private void OnContextChanged(object sender, XObjectChangeEventArgs e)
            {
                if (m_filter == null)
                {
                    TrySelectNextValue();
                }
                else
                {
                    // cache and reset change object
                    XElementChange change = m_change;
                    m_change = new XElementChange();

                    if (m_filter(change))
                    {
                        TrySelectNextValue();
                    }
                }
            }

            private void TrySelectNextValue()
            {
                try
                {
                    T result = m_selector();
                    if (m_comparer == null || !m_comparer.Equals(result, m_lastResult))
                    {
                        m_lastResult = result;
                    }
                }
                catch (Exception ex)
                {
                    m_observer.OnError(ex);
                    return;
                }

                m_observer.OnNext(m_lastResult);
            }

        }


        /// <summary>
        /// XAttributes have very useful explicit conversion functions, that can handle attributes being null. Pity there's no
        /// generic access to them. This class provides a workaround - caches all op_Explicit functions and creates delegates
        /// for used ones.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal static class ExplicitCast<T>
        {
            public static T From<U>(U from)
            {
                return Conversion<U, T>.Call(from);
            }

            private static class Converters<From>
            {
                public static readonly MethodInfo[] Methods;

                static Converters()
                {
                    Methods = typeof(From).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(x => x.Name == "op_Explicit").ToArray();
                }
            }

            private static class Conversion<From, To>
            {
                public static readonly Func<From, To> Call;

                static Conversion()
                {
                    try
                    {
                        var found = Converters<From>.Methods.Single(x => x.ReturnType == typeof(To));
                        Call = (Func<From, To>)found.CreateDelegate(typeof(Func<From, To>));
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentOutOfRangeException("No such conversion: " + typeof(From) + " to " + typeof(To), ex);
                    }
                }
            }
        }

        /// <summary>
        /// Runs Enumerable.SequenceEqual on passed ranges.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal class SequenceEqualEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
        {
            public static SequenceEqualEqualityComparer<T> Instance = new SequenceEqualEqualityComparer<T>();

            public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
            {
                if (Object.ReferenceEquals(x, y))
                {
                    return true;
                }
                else if (x == null || y == null)
                {
                    return false;
                }
                else
                {
                    return x.SequenceEqual(y);
                }
            }

            public int GetHashCode(IEnumerable<T> obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
