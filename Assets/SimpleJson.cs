using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum JSONValueType
{
    String,
    Number,
    Object,
    Array,
    Boolean,
    Number64,
    Null
}

public enum JSONNodeType
{
    Array = 1,
    Object = 2,
    String = 3,
    Number = 4,
    NullValue = 5,
    Boolean = 6,
    None = 7,
    Custom = 0xFF,
}
public enum JSONTextMode
{
    Compact,
    Indent
}

public abstract partial class JSONValue
{
    #region Enumerators
    public struct Enumerator
    {
        private enum Type { None, Array, Object }
        private Type type;
        private Dictionary<string, JSONValue>.Enumerator m_Object;
        private List<JSONValue>.Enumerator m_Array;
        public bool IsValid { get { return type != Type.None; } }
        public Enumerator(List<JSONValue>.Enumerator aArrayEnum)
        {
            type = Type.Array;
            m_Object = default(Dictionary<string, JSONValue>.Enumerator);
            m_Array = aArrayEnum;
        }
        public Enumerator(Dictionary<string, JSONValue>.Enumerator aDictEnum)
        {
            type = Type.Object;
            m_Object = aDictEnum;
            m_Array = default(List<JSONValue>.Enumerator);
        }
        public KeyValuePair<string, JSONValue> Current
        {
            get
            {
                if (type == Type.Array)
                    return new KeyValuePair<string, JSONValue>(string.Empty, m_Array.Current);
                else if (type == Type.Object)
                    return m_Object.Current;
                return new KeyValuePair<string, JSONValue>(string.Empty, null);
            }
        }
        public bool MoveNext()
        {
            if (type == Type.Array)
                return m_Array.MoveNext();
            else if (type == Type.Object)
                return m_Object.MoveNext();
            return false;
        }
    }
    public struct ValueEnumerator
    {
        private Enumerator m_Enumerator;
        public ValueEnumerator(List<JSONValue>.Enumerator aArrayEnum) : this(new Enumerator(aArrayEnum)) { }
        public ValueEnumerator(Dictionary<string, JSONValue>.Enumerator aDictEnum) : this(new Enumerator(aDictEnum)) { }
        public ValueEnumerator(Enumerator aEnumerator) { m_Enumerator = aEnumerator; }
        public JSONValue Current { get { return m_Enumerator.Current.Value; } }
        public bool MoveNext() { return m_Enumerator.MoveNext(); }
        public ValueEnumerator GetEnumerator() { return this; }
    }
    public struct KeyEnumerator
    {
        private Enumerator m_Enumerator;
        public KeyEnumerator(List<JSONValue>.Enumerator aArrayEnum) : this(new Enumerator(aArrayEnum)) { }
        public KeyEnumerator(Dictionary<string, JSONValue>.Enumerator aDictEnum) : this(new Enumerator(aDictEnum)) { }
        public KeyEnumerator(Enumerator aEnumerator) { m_Enumerator = aEnumerator; }
        public JSONValue Current { get { return m_Enumerator.Current.Key; } }
        public bool MoveNext() { return m_Enumerator.MoveNext(); }
        public KeyEnumerator GetEnumerator() { return this; }
    }

    //private static readonly Regex unicodeRegex = new Regex(@"\\u([0-9a-fA-F]{4})");
    //private static readonly byte[] unicodeBytes = new byte[2];

    public class LinqEnumerator : IEnumerator<KeyValuePair<string, JSONValue>>, IEnumerable<KeyValuePair<string, JSONValue>>
    {
        private JSONValue m_Node;
        private Enumerator m_Enumerator;
        internal LinqEnumerator(JSONValue aNode)
        {
            m_Node = aNode;
            if (m_Node != null)
                m_Enumerator = m_Node.GetEnumerator();
        }
        public KeyValuePair<string, JSONValue> Current { get { return m_Enumerator.Current; } }
        object IEnumerator.Current { get { return m_Enumerator.Current; } }
        public bool MoveNext() { return m_Enumerator.MoveNext(); }

        public void Dispose()
        {
            m_Node = null;
            m_Enumerator = new Enumerator();
        }

        public IEnumerator<KeyValuePair<string, JSONValue>> GetEnumerator()
        {
            return new LinqEnumerator(m_Node);
        }

        public void Reset()
        {
            if (m_Node != null)
                m_Enumerator = m_Node.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new LinqEnumerator(m_Node);
        }
    }

    #endregion Enumerators

    #region common interface

    public static bool forceASCII = false; // Use Unicode by default

    public abstract JSONNodeType Tag { get; }

    public virtual JSONValue this[int aIndex] { get { return null; } set { } }

    public virtual JSONValue this[string aKey] { get { return null; } set { } }

    public virtual string Value { get { return ""; } set { } }

    public virtual int Length { get { return 0; } }

    public virtual bool IsNumber { get { return false; } }
    public virtual bool IsNumber64 { get { return false; } }
    public virtual bool IsString { get { return false; } }
    public virtual bool IsBoolean { get { return false; } }
    public virtual bool IsNull { get { return false; } }
    public virtual bool IsArray { get { return false; } }
    public virtual bool IsObject { get { return false; } }

    public virtual bool Inline { get { return false; } set { } }

    public virtual void Add(string aKey, JSONValue aItem)
    {
    }
    public virtual void Add(JSONValue aItem)
    {
        if(aItem == this)
        {
            throw new Exception();
        }
        Add("", aItem);
    }

    public virtual JSONValue Remove(string aKey)
    {
        return null;
    }

    public virtual JSONValue Remove(int aIndex)
    {
        return null;
    }

    public virtual JSONValue Remove(JSONValue aNode)
    {
        return aNode;
    }

    public virtual IEnumerable<JSONValue> Children
    {
        get
        {
            yield break;
        }
    }

    public IEnumerable<JSONValue> DeepChildren
    {
        get
        {
            foreach (var C in Children)
                foreach (var D in C.DeepChildren)
                    yield return D;
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        WriteToStringBuilder(sb, 0, 0, JSONTextMode.Compact, false);
        return sb.ToString();
    }

    public string ToString(bool includeExcape)
    {
        StringBuilder sb = new StringBuilder();
        WriteToStringBuilder(sb, 0, 0, JSONTextMode.Compact, true);
        return sb.ToString();
    }

    public virtual string ToString(int aIndent)
    {
        StringBuilder sb = new StringBuilder();
        WriteToStringBuilder(sb, 0, aIndent, JSONTextMode.Indent, false);
        return sb.ToString();
    }
    internal abstract void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr);

    public abstract Enumerator GetEnumerator();
    public IEnumerable<KeyValuePair<string, JSONValue>> Linq { get { return new LinqEnumerator(this); } }
    public KeyEnumerator Keys { get { return new KeyEnumerator(GetEnumerator()); } }
    public ValueEnumerator Values { get { return new ValueEnumerator(GetEnumerator()); } }

    #endregion common interface

    #region typecasting properties

    public JSONValueType Type
    {
        get
        {
            if (IsString)
            {
                return JSONValueType.String;
            }
            else if (IsArray)
            {
                return JSONValueType.Array;
            }
            else if (IsNumber)
            {
                return JSONValueType.Number;
            }
            else if (IsNumber64)
            {
                return JSONValueType.Number64;
            }
            else if (IsObject)
            {
                return JSONValueType.Object;
            }
            else if (IsBoolean)
            {
                return JSONValueType.Boolean;
            }
            else
                return JSONValueType.Null;
        }
    }

    public virtual double Number
    {
        get
        {
            double v = 0.0;
            if (double.TryParse(Value, out v))
                return v;
            return 0.0;
        }
        set
        {
            Value = value.ToString();
        }
    }

    public virtual long Number64
    {
        get
        {
            long v = 0;
            if (long.TryParse(Value, out v))
                return v;
            return 0;
        }
        set
        {
            Value = value.ToString();
        }
    }

    public virtual bool IsInt()
    {
        int v = 0;
        if (int.TryParse(Value, out v))
        {
            return true;
        }
        return false;
    }

    //public virtual bool IsFloat()
    //{
    //    Value = "0.1";

    //    int v2 = 0;
    //    if(int.TryParse(Value, out v2))
    //    {
    //        return false;
    //    }

    //    float v = 0f;
    //    if(float.TryParse(Value, out v))
    //    {
    //        return true;
    //    }
    //    return false;
    //}

    public virtual string Str
    {
        get
        {
            return Value;
        }
        set { Value = Value; }
    }

    public virtual int Int
    {
        get { return (int)Number; }
        set { Number = value; }
    }

    public virtual uint UInt
    {
        get { return (uint)Int; }
        set { Number = value; }
    }

    public virtual long Long
    {
        get { return (long)Number64; }
        set { Number64 = value; }
    }
    public virtual ulong ULong
    {
        get { return (ulong)Number64; }
        set { Number64 = (long)value; }
    }

    public virtual float Float
    {
        get { return (float)Number; }
        set { Number = value; }
    }

    public virtual bool Boolean
    {
        get
        {
            bool v = false;
            if (bool.TryParse(Value, out v))
                return v;
            return !string.IsNullOrEmpty(Value);
        }
        set
        {
            Value = (value) ? "true" : "false";
        }
    }

    public virtual JSONArray Array
    {
        get
        {
            return this as JSONArray;
        }
    }

    public virtual JSONObject Obj
    {
        get
        {
            return this as JSONObject;
        }
    }


    #endregion typecasting properties

    #region operators

    public static implicit operator JSONValue(string s)
    {
        return new JSONString(s);
    }
    public static implicit operator string(JSONValue d)
    {
        return (d == null) ? null : d.Value;
    }

    public static implicit operator JSONValue(double n)
    {
        return new JSONNumber(n);
    }
    public static implicit operator double(JSONValue d)
    {
        return (d == null) ? 0 : d.Number;
    }

    public static implicit operator JSONValue(long l)
    {
        return new JSONumber64(l);
    }

    public static implicit operator JSONValue(ulong l)
    {
        return new JSONumber64((long)l);
    }

    public static implicit operator JSONValue(float n)
    {
        return new JSONNumber(n);
    }
    public static implicit operator float(JSONValue d)
    {
        return (d == null) ? 0 : d.Float;
    }

    public static implicit operator JSONValue(int n)
    {
        return new JSONNumber(n);
    }
    public static implicit operator JSONValue(uint n)
    {
        return new JSONNumber(n);
    }

    public static implicit operator int(JSONValue d)
    {
        return (d == null) ? 0 : d.Int;
    }

    public static implicit operator JSONValue(bool b)
    {
        return new JSONBool(b);
    }
    public static implicit operator bool(JSONValue d)
    {
        return (d == null) ? false : d.Boolean;
    }

    public static implicit operator JSONValue(KeyValuePair<string, JSONValue> aKeyValue)
    {
        return aKeyValue.Value;
    }

    public static bool operator ==(JSONValue a, object b)
    {
        if (ReferenceEquals(a, b))
            return true;
        bool aIsNull = a is JSONNull || ReferenceEquals(a, null) || a is JSONLazyCreator;
        bool bIsNull = b is JSONNull || ReferenceEquals(b, null) || b is JSONLazyCreator;
        if (aIsNull && bIsNull)
            return true;
        return !aIsNull && a.Equals(b);
    }

    public static bool operator !=(JSONValue a, object b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    #endregion operators

    [ThreadStatic]
    private static StringBuilder m_EscapeBuilder;
    internal static StringBuilder EscapeBuilder
    {
        get
        {
            if (m_EscapeBuilder == null)
                m_EscapeBuilder = new StringBuilder();
            return m_EscapeBuilder;
        }
    }
    internal static string Escape(string aText, bool jsonStr)
    {
        var sb = EscapeBuilder;
        sb.Length = 0;
        if(aText == null)
        {
            return "";
        }
        if (sb.Capacity < aText.Length + aText.Length / 10)
            sb.Capacity = aText.Length + aText.Length / 10;

        if (aText != null)
        {
            foreach (char c in aText)
            {
                switch (c)
                {
                    case '\\':
                        if (jsonStr)
                        {
                            sb.Append("\\\\");
                        }
                        break;
                    case '\"':
                        //if (jsonStr)
                        {
                            sb.Append("\\\"");
                        }
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    default:
                        if (c < ' ' || (forceASCII && c > 127))
                        {
                            ushort val = c;
                            sb.Append("\\u").Append(val.ToString("X4"));
                        }
                        else
                            sb.Append(c);
                        break;
                }
            }
        }
        string result = sb.ToString();
        sb.Length = 0;
        return result;
    }

    static void ParseElement(JSONValue ctx, string token, string tokenName, bool quoted)
    {
        if (quoted)
        {
            ctx.Add(tokenName, token);
            return;
        }
        string tmp = token.ToLower();
        if (tmp == "false" || tmp == "true")
            ctx.Add(tokenName, tmp == "true");
        else if (tmp == "null")
            ctx.Add(tokenName, null);
        else
        {
            int intVal = 0;
            long uLong = 0;
            double val = 0;

            if (int.TryParse(token, out intVal))
            {
                ctx.Add(tokenName, intVal);
            }
            else if (long.TryParse(token, out uLong))
            {
                ctx.Add(tokenName, uLong);
            }
            else
            {
                if (double.TryParse(token, out val))
                    ctx.Add(tokenName, val);
                else
                    ctx.Add(tokenName, token);
            }
        }
    }

    public static JSONObject Parse(string aJSON)
    {
        if (aJSON == null) return null;

        Stack<JSONValue> stack = new Stack<JSONValue>();
        JSONValue ctx = null;
        int i = 0;
        StringBuilder Token = new StringBuilder();
        string TokenName = "";
        bool QuoteMode = false;
        bool TokenIsQuoted = false;
        while (i < aJSON.Length)
        {
            switch (aJSON[i])
            {
                case '{':
                    if (QuoteMode)
                    {
                        Token.Append(aJSON[i]);
                        break;
                    }
                    stack.Push(new JSONObject());
                    if (ctx != null)
                    {
                        ctx.Add(TokenName, stack.Peek());
                    }
                    TokenName = "";
                    Token.Length = 0;
                    ctx = stack.Peek();
                    break;

                case '[':
                    if (QuoteMode)
                    {
                        Token.Append(aJSON[i]);
                        break;
                    }

                    stack.Push(new JSONArray());
                    if (ctx != null)
                    {
                        ctx.Add(TokenName, stack.Peek());
                    }
                    TokenName = "";
                    Token.Length = 0;
                    ctx = stack.Peek();
                    break;

                case '}':
                case ']':
                    if (QuoteMode)
                    {

                        Token.Append(aJSON[i]);
                        break;
                    }
                    if (stack.Count == 0)
                    {
                        UnityEngine.Debug.Log(aJSON);
                        throw new Exception("JSON Parse: Too many closing brackets");
                    }

                    stack.Pop();
                    if (Token.Length > 0 || TokenIsQuoted)
                    {
                        ParseElement(ctx, Token.ToString(), TokenName, TokenIsQuoted);
                        TokenIsQuoted = false;
                    }
                    TokenName = "";
                    Token.Length = 0;
                    if (stack.Count > 0)
                        ctx = stack.Peek();
                    break;

                case ':':
                    if (QuoteMode)
                    {
                        Token.Append(aJSON[i]);
                        break;
                    }
                    TokenName = Token.ToString();
                    Token.Length = 0;
                    TokenIsQuoted = false;
                    break;

                case '"':
                    QuoteMode ^= true;
                    TokenIsQuoted |= QuoteMode;
                    break;

                case ',':
                    if (QuoteMode)
                    {
                        Token.Append(aJSON[i]);
                        break;
                    }
                    if (Token.Length > 0 || TokenIsQuoted)
                    {
                        ParseElement(ctx, Token.ToString(), TokenName, TokenIsQuoted);
                        TokenIsQuoted = false;
                    }
                    TokenName = "";
                    Token.Length = 0;
                    TokenIsQuoted = false;
                    break;

                case '\r':
                case '\n':
                    break;

                case ' ':
                case '\t':
                    if (QuoteMode)
                        Token.Append(aJSON[i]);
                    break;

                case '\\':
                    ++i;
                    if (QuoteMode)
                    {
                        char C = aJSON[i];
                        switch (C)
                        {
                            case 't':
                                Token.Append('\t');
                                break;
                            case 'r':
                                Token.Append('\r');
                                break;
                            case 'n':
                                Token.Append('\n');
                                break;
                            case 'b':
                                Token.Append('\b');
                                break;
                            case 'f':
                                Token.Append('\f');
                                break;
                            case 'u':
                                {
                                    string s = aJSON.Substring(i + 1, 4);
                                    Token.Append((char)int.Parse(
                                        s,
                                        System.Globalization.NumberStyles.AllowHexSpecifier));
                                    i += 4;
                                    break;
                                }
                            default:
                                Token.Append(C);
                                break;
                        }
                    }
                    break;

                default:
                    Token.Append(aJSON[i]);
                    break;
            }
            ++i;
        }
        if (QuoteMode)
        {
            throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
        }
        return ctx as JSONObject;
    }


    public virtual string ToBeautifyString(int tabCount = 0, bool isArray = false)
    {
#if UNITY_EDITOR
        switch (Type)
        {
            case JSONValueType.Object:
                return Obj.ToBeautifyString(tabCount, isArray);

            case JSONValueType.Array:
                return Array.ToBeautifyString(tabCount);

            case JSONValueType.Boolean:
                return InsertTab(isArray, tabCount) + (Boolean ? "true" : "false");

            case JSONValueType.Number:
                return InsertTab(isArray, tabCount) + Number.ToString();

            case JSONValueType.String:
                return InsertTab(isArray, tabCount) + "\"" + Str + "\"";

            case JSONValueType.Null:
                return InsertTab(isArray, tabCount) + "null";
        }
        return "null";
#else
        return ToString();
#endif
    }

#if UNITY_EDITOR
    string InsertTab(bool isArray, int tabCount)
    {
        if (isArray == false) return "";

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < tabCount; ++i)
            sb.Append("    ");

        return sb.ToString();
    }
#endif
}
// End of JSONNode

public partial class JSONArray : JSONValue
{
    private List<JSONValue> m_List = new List<JSONValue>();
    private bool inline = false;
    public override bool Inline
    {
        get { return inline; }
        set { inline = value; }
    }

    public override JSONNodeType Tag { get { return JSONNodeType.Array; } }
    public override bool IsArray { get { return true; } }
    public override Enumerator GetEnumerator() { return new Enumerator(m_List.GetEnumerator()); }

    public override JSONValue this[int aIndex]
    {
        get
        {
            if (aIndex < 0 || aIndex >= m_List.Count)
                return new JSONLazyCreator(this);
            return m_List[aIndex];
        }
        set
        {
            if (value == null)
                value = JSONNull.CreateOrGet();
            if (aIndex < 0 || aIndex >= m_List.Count)
                m_List.Add(value);
            else
                m_List[aIndex] = value;
        }
    }

    public override JSONValue this[string aKey]
    {
        get { return new JSONLazyCreator(this); }
        set
        {
            if (value == null)
                value = JSONNull.CreateOrGet();
            m_List.Add(value);
        }
    }

    public override int Length
    {
        get { return m_List.Count; }
    }

    public override void Add(string aKey, JSONValue aItem)
    {
        if (aItem == null)
            aItem = JSONNull.CreateOrGet();
        m_List.Add(aItem);
    }

    public override JSONValue Remove(int aIndex)
    {
        if (aIndex < 0 || aIndex >= m_List.Count)
            return null;
        JSONValue tmp = m_List[aIndex];
        m_List.RemoveAt(aIndex);
        return tmp;
    }

    public override JSONValue Remove(JSONValue aNode)
    {
        m_List.Remove(aNode);
        return aNode;
    }

    public override IEnumerable<JSONValue> Children
    {
        get
        {
            foreach (JSONValue N in m_List)
                yield return N;
        }
    }


    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr)
    {
        aSB.Append('[');
        int count = m_List.Count;
        if (inline)
            aMode = JSONTextMode.Compact;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                aSB.Append(',');
            if (aMode == JSONTextMode.Indent)
                aSB.AppendLine();

            if (aMode == JSONTextMode.Indent)
                aSB.Append(' ', aIndent + aIndentInc);
            m_List[i].WriteToStringBuilder(aSB, aIndent + aIndentInc, aIndentInc, aMode, isJsonStr);
        }
        if (aMode == JSONTextMode.Indent)
            aSB.AppendLine().Append(' ', aIndent);
        aSB.Append(']');
    }

    public static new JSONArray Parse(string jsonString)
    {
        var tempObject = JSONObject.Parse("{ \"array\" :" + jsonString + '}');
        return tempObject == null ? null : tempObject.GetValue("array").Array;
    }

#if UNITY_EDITOR
    public string ToBeautifyString(int tabCount)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append('[');
        stringBuilder.AppendLine();

        for (int i = 0, iMax = m_List.Count; i < iMax; ++i)
        {
            stringBuilder.Append(m_List[i].ToBeautifyString(tabCount + 1, true));

            if (i < iMax - 1)
                stringBuilder.Append(',');

            stringBuilder.AppendLine();
        }

        InsertTab(stringBuilder, tabCount);
        stringBuilder.Append(']');
        return stringBuilder.ToString();
    }

    void InsertTab(StringBuilder sb, int tabCount)
    {
        for (int i = 0; i < tabCount; ++i)
            sb.Append("    ");
    }
#endif

    public string[] ToArrayString()
    {
        List<string> list = new List<string>();

        for (int i = 0; i < m_List.Count; i++)
        {
            if (m_List[i].Type == JSONValueType.String)
            {
                list.Add(m_List[i].Str);
            }
        }
        return list.ToArray();
    }

    public int[] ToArrayInt()
    {
        List<int> list = new List<int>();

        for (int i = 0; i < m_List.Count; i++)
        {
            if (m_List[i].Type == JSONValueType.Number)
            {
                list.Add(m_List[i].Int);
            }
        }
        return list.ToArray();
    }

    public float[] ToArrayFloat()
    {
        List<float> list = new List<float>();

        for (int i = 0; i < m_List.Count; i++)
        {
            if (m_List[i].Type == JSONValueType.Number)
            {
                list.Add(m_List[i].Float);
            }
        }
        return list.ToArray();
    }
}
// End of JSONArray

public partial class JSONObject : JSONValue
{
    private Dictionary<string, JSONValue> m_Dict = new Dictionary<string, JSONValue>();

    private bool inline = false;
    public override bool Inline
    {
        get { return inline; }
        set { inline = value; }
    }

    public override JSONNodeType Tag { get { return JSONNodeType.Object; } }
    public override bool IsObject { get { return true; } }

    public override Enumerator GetEnumerator() { return new Enumerator(m_Dict.GetEnumerator()); }


    public override JSONValue this[string aKey]
    {
        get
        {
            if (m_Dict.ContainsKey(aKey))
                return m_Dict[aKey];
            else
                return new JSONLazyCreator(this, aKey);
        }
        set
        {
            if (value == null)
                value = JSONNull.CreateOrGet();
            if (m_Dict.ContainsKey(aKey))
                m_Dict[aKey] = value;
            else
                m_Dict.Add(aKey, value);
        }
    }

    public override JSONValue this[int aIndex]
    {
        get
        {
            if (aIndex < 0 || aIndex >= m_Dict.Count)
                return null;
            return m_Dict.ElementAt(aIndex).Value;
        }
        set
        {
            if (value == null)
                value = JSONNull.CreateOrGet();
            if (aIndex < 0 || aIndex >= m_Dict.Count)
                return;
            string key = m_Dict.ElementAt(aIndex).Key;
            m_Dict[key] = value;
        }
    }

    public override int Length
    {
        get { return m_Dict.Count; }
    }

    public override void Add(string aKey, JSONValue aItem)
    {
        if (aItem == null)
            aItem = JSONNull.CreateOrGet();

        if (!string.IsNullOrEmpty(aKey))
        {
            if (m_Dict.ContainsKey(aKey))
                m_Dict[aKey] = aItem;
            else
                m_Dict.Add(aKey, aItem);
        }
        else
            m_Dict.Add(Guid.NewGuid().ToString(), aItem);
    }

    public override JSONValue Remove(string aKey)
    {
        if (!m_Dict.ContainsKey(aKey))
            return null;
        JSONValue tmp = m_Dict[aKey];
        m_Dict.Remove(aKey);
        return tmp;
    }

    public override JSONValue Remove(int aIndex)
    {
        if (aIndex < 0 || aIndex >= m_Dict.Count)
            return null;
        var item = m_Dict.ElementAt(aIndex);
        m_Dict.Remove(item.Key);
        return item.Value;
    }

    public override JSONValue Remove(JSONValue aNode)
    {
        try
        {
            var item = m_Dict.Where(k => k.Value == aNode).First();
            m_Dict.Remove(item.Key);
            return aNode;
        }
        catch
        {
            return null;
        }
    }

    public override IEnumerable<JSONValue> Children
    {
        get
        {
            foreach (KeyValuePair<string, JSONValue> N in m_Dict)
                yield return N.Value;
        }
    }

    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr)
    {
        aSB.Append('{');
        bool first = true;
        if (inline)
            aMode = JSONTextMode.Compact;
        foreach (var k in m_Dict)
        {
            if (!first)
                aSB.Append(',');
            first = false;
            if (aMode == JSONTextMode.Indent)
                aSB.AppendLine();
            if (aMode == JSONTextMode.Indent)
                aSB.Append(' ', aIndent + aIndentInc);
            aSB.Append('\"').Append(Escape(k.Key, isJsonStr)).Append('\"');
            if (aMode == JSONTextMode.Compact)
                aSB.Append(':');
            else
                aSB.Append(" : ");
            k.Value.WriteToStringBuilder(aSB, aIndent + aIndentInc, aIndentInc, aMode, isJsonStr);
        }
        if (aMode == JSONTextMode.Indent)
            aSB.AppendLine().Append(' ', aIndent);
        aSB.Append('}');
    }

    public JSONValue GetValue(string key)
    {
        JSONValue value;
        m_Dict.TryGetValue(key, out value);
        return value;
    }

    public string GetString(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return string.Empty;
        }
        return value;
    }

    //public decimal GetNumber(string key)
    //{
    //    var value = GetValue(key);
    //    if (value == null)
    //    {
    //        return decimal.MaxValue;
    //    }
    //    return value.Number;
    //}

    public float GetFloat(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return float.NaN;
        }
        return value.Float;
    }

    public long GetLong(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return 0;
        }
        return value.Long;
    }

    public ulong GetUlong(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return 0;
        }
        return (ulong)value.Long;
    }

    public int GetInt(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return 0;
        }
        return value.Int;
    }

    public uint GetUInt(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return 0;
        }
        return value.UInt;
    }

    public JSONObject GetObject(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return null;
        }
        return value.Obj;
    }

    public bool GetBoolean(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return false;
        }
        return value.Boolean;
    }

    public JSONArray GetArray(string key)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return null;
        }
        return value.Array;
    }

    public bool ContainsKey(string key)
    {
        return m_Dict.ContainsKey(key);
    }

    public override string ToBeautifyString(int tabCount = 0, bool isArray = false)
    {
#if UNITY_EDITOR
        var stringBuilder = new StringBuilder();

        InsertTab(stringBuilder, isArray ? tabCount : tabCount - 1);

        stringBuilder.Append('{');
        stringBuilder.AppendLine();

        int count = 0;
        int maxCount = m_Dict.Count;

        foreach (var pair in m_Dict)
        {
            InsertTab(stringBuilder, tabCount + 1);

            stringBuilder.Append("\"" + pair.Key + "\": ");
            stringBuilder.Append(pair.Value.ToBeautifyString(tabCount + 1, false));

            if (count < maxCount - 1)
                stringBuilder.Append(',');

            stringBuilder.AppendLine();
            count++;
        }

        InsertTab(stringBuilder, tabCount);
        stringBuilder.Append('}');
        return stringBuilder.ToString();
#else
        return ToString();
#endif
    }

#if UNITY_EDITOR
    void InsertTab(StringBuilder sb, int tabCount)
    {
        for (int i = 0; i < tabCount; ++i)
            sb.Append("    ");
    }
#endif
}
// End of JSONObject

public partial class JSONString : JSONValue
{
    private string m_Data;

    public override JSONNodeType Tag { get { return JSONNodeType.String; } }
    public override bool IsString { get { return true; } }

    public override Enumerator GetEnumerator() { return new Enumerator(); }


    public override string Value
    {
        get { return m_Data; }
        set
        {
            m_Data = value;
        }
    }

    public JSONString(string aData)
    {
        m_Data = aData;
    }

    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr)
    {
        aSB.Append('\"').Append(Escape(m_Data, isJsonStr)).Append('\"');
    }
    public override bool Equals(object obj)
    {
        if (base.Equals(obj))
            return true;
        string s = obj as string;
        if (s != null)
            return m_Data == s;
        JSONString s2 = obj as JSONString;
        if (s2 != null)
            return m_Data == s2.m_Data;
        return false;
    }
    public override int GetHashCode()
    {
        return m_Data.GetHashCode();
    }
}
// End of JSONString
public partial class JSONumber64 : JSONValue
{
    private long m_Data;

    public override JSONNodeType Tag { get { return JSONNodeType.Number; } }
    public override bool IsNumber64 { get { return true; } }
    public override Enumerator GetEnumerator() { return new Enumerator(); }

    public override string Value
    {
        get { return m_Data.ToString(); }
        set
        {
            long v;
            if (long.TryParse(value, out v))
                m_Data = v;
        }
    }

    public override long Number64
    {
        get { return m_Data; }
        set { m_Data = value; }
    }

    public JSONumber64(long aData)
    {
        m_Data = aData;
    }

    public JSONumber64(string aData)
    {
        Value = aData;
    }

    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr)
    {
        aSB.Append(m_Data);
    }
    private static bool IsNumeric(object value)
    {
        return value is int || value is uint
            || value is float || value is double
            || value is decimal
            || value is long || value is ulong
            || value is short || value is ushort
            || value is sbyte || value is byte;
    }
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        if (base.Equals(obj))
            return true;
        JSONumber64 s2 = obj as JSONumber64;
        if (s2 != null)
            return m_Data == s2.m_Data;
        if (IsNumeric(obj))
            return Convert.ToInt64(obj) == m_Data;
        return false;
    }
    public override int GetHashCode()
    {
        return m_Data.GetHashCode();
    }
}

public partial class JSONNumber : JSONValue
{
    private double m_Data;

    public override JSONNodeType Tag { get { return JSONNodeType.Number; } }
    public override bool IsNumber { get { return true; } }
    public override Enumerator GetEnumerator() { return new Enumerator(); }

    public override string Value
    {
        get { return m_Data.ToString(); }
        set
        {
            double v;
            if (double.TryParse(value, out v))
                m_Data = v;
        }
    }

    public override double Number
    {
        get { return m_Data; }
        set { m_Data = value; }
    }

    public JSONNumber(double aData)
    {
        m_Data = aData;
    }

    public JSONNumber(string aData)
    {
        Value = aData;
    }

    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr)
    {
        aSB.Append(m_Data);
    }
    private static bool IsNumeric(object value)
    {
        return value is int || value is uint
            || value is float || value is double
            || value is decimal
            || value is long || value is ulong
            || value is short || value is ushort
            || value is sbyte || value is byte;
    }
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        if (base.Equals(obj))
            return true;
        JSONNumber s2 = obj as JSONNumber;
        if (s2 != null)
            return m_Data == s2.m_Data;
        if (IsNumeric(obj))
            return Convert.ToDouble(obj) == m_Data;
        return false;
    }
    public override int GetHashCode()
    {
        return m_Data.GetHashCode();
    }
}
// End of JSONNumber

public partial class JSONBool : JSONValue
{
    private bool m_Data;

    public override JSONNodeType Tag { get { return JSONNodeType.Boolean; } }
    public override bool IsBoolean { get { return true; } }
    public override Enumerator GetEnumerator() { return new Enumerator(); }

    public override string Value
    {
        get { return m_Data.ToString(); }
        set
        {
            bool v;
            if (bool.TryParse(value, out v))
                m_Data = v;
        }
    }
    public override bool Boolean
    {
        get { return m_Data; }
        set { m_Data = value; }
    }

    public JSONBool(bool aData)
    {
        m_Data = aData;
    }

    public JSONBool(string aData)
    {
        Value = aData;
    }

    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr)
    {
        aSB.Append((m_Data) ? "true" : "false");
    }
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        if (obj is bool)
            return m_Data == (bool)obj;
        return false;
    }
    public override int GetHashCode()
    {
        return m_Data.GetHashCode();
    }
}
// End of JSONBool

public partial class JSONNull : JSONValue
{
    static JSONNull m_StaticInstance = new JSONNull();
    public static bool reuseSameInstance = true;
    public static JSONNull CreateOrGet()
    {
        if (reuseSameInstance)
            return m_StaticInstance;
        return new JSONNull();
    }
    private JSONNull() { }

    public override JSONNodeType Tag { get { return JSONNodeType.NullValue; } }
    public override bool IsNull { get { return true; } }
    public override Enumerator GetEnumerator() { return new Enumerator(); }

    public override string Value
    {
        get { return "null"; }
        set { }
    }
    public override bool Boolean
    {
        get { return false; }
        set { }
    }

    public override bool Equals(object obj)
    {
        if (object.ReferenceEquals(this, obj))
            return true;
        return (obj is JSONNull);
    }
    public override int GetHashCode()
    {
        return 0;
    }

    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr)
    {
        aSB.Append("null");
    }
}
// End of JSONNull

internal partial class JSONLazyCreator : JSONValue
{
    private JSONValue m_Node = null;
    private string m_Key = null;
    public override JSONNodeType Tag { get { return JSONNodeType.None; } }
    public override Enumerator GetEnumerator() { return new Enumerator(); }

    public JSONLazyCreator(JSONValue aNode)
    {
        m_Node = aNode;
        m_Key = null;
    }

    public JSONLazyCreator(JSONValue aNode, string aKey)
    {
        m_Node = aNode;
        m_Key = aKey;
    }

    private void Set(JSONValue aVal)
    {
        if (m_Key == null)
        {
            m_Node.Add(aVal);
        }
        else
        {
            m_Node.Add(m_Key, aVal);
        }
        m_Node = null; // Be GC friendly.
    }

    public override JSONValue this[int aIndex]
    {
        get
        {
            return new JSONLazyCreator(this);
        }
        set
        {
            var tmp = new JSONArray();
            tmp.Add(value);
            Set(tmp);
        }
    }

    public override JSONValue this[string aKey]
    {
        get
        {
            return new JSONLazyCreator(this, aKey);
        }
        set
        {
            var tmp = new JSONObject();
            tmp.Add(aKey, value);
            Set(tmp);
        }
    }

    public override void Add(JSONValue aItem)
    {
        var tmp = new JSONArray();
        tmp.Add(aItem);
        Set(tmp);
    }

    public override void Add(string aKey, JSONValue aItem)
    {
        var tmp = new JSONObject();
        tmp.Add(aKey, aItem);
        Set(tmp);
    }

    public static bool operator ==(JSONLazyCreator a, object b)
    {
        if (b == null)
            return true;
        return System.Object.ReferenceEquals(a, b);
    }

    public static bool operator !=(JSONLazyCreator a, object b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return true;
        return System.Object.ReferenceEquals(this, obj);
    }

    public override int GetHashCode()
    {
        return 0;
    }

    public override int Int
    {
        get
        {
            JSONNumber tmp = new JSONNumber(0);
            Set(tmp);
            return 0;
        }
        set
        {
            JSONNumber tmp = new JSONNumber(value);
            Set(tmp);
        }
    }

    public override float Float
    {
        get
        {
            JSONNumber tmp = new JSONNumber(0.0f);
            Set(tmp);
            return 0.0f;
        }
        set
        {
            JSONNumber tmp = new JSONNumber(value);
            Set(tmp);
        }
    }

    public override double Number
    {
        get
        {
            JSONNumber tmp = new JSONNumber(0.0);
            Set(tmp);
            return 0.0;
        }
        set
        {
            JSONNumber tmp = new JSONNumber(value);
            Set(tmp);
        }
    }

    public override bool Boolean
    {
        get
        {
            JSONBool tmp = new JSONBool(false);
            Set(tmp);
            return false;
        }
        set
        {
            JSONBool tmp = new JSONBool(value);
            Set(tmp);
        }
    }

    public override JSONArray Array
    {
        get
        {
            JSONArray tmp = new JSONArray();
            Set(tmp);
            return tmp;
        }
    }

    public override JSONObject Obj
    {
        get
        {
            JSONObject tmp = new JSONObject();
            Set(tmp);
            return tmp;
        }
    }
    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode, bool isJsonStr)
    {
        aSB.Append("null");
    }
}
// End of JSONLazyCreator

public static class JSON
{
    public static JSONValue Parse(string aJSON)
    {
        return JSONValue.Parse(aJSON);
    }
}
