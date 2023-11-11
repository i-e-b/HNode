using System.Text;

namespace HNodeDotnet;


/**
 * Very simple and liberal HTML parser for templating
 * The tree we output is just a set of ranges over the original string.
 * */
public class HNode {
    /** The entire document */
    public const int TypeRoot = 0;
    /** Text or raw data. Doesn't look like mark-up */
    public const int TypeText = 1;
    /** Normal element. Looks like &lt;elem attr>...&lt;/elem> */
    public const int TypeNode = 2;
    /** Self-closed element. Looks like &lt;elem attr/> */
    public const int TypeElem = 3;
    /** Self-enclosed directive. Looks like &lt;!directive...>*/
    public const int TypeDirk = 4;
    /** "Comment" or script. Looks like &lt;!-- ... ---> or &lt;script>...&lt;/script>*/
    public const int TypeSkit = 5;

    /** Child nodes. Can be empty, never null */
    public readonly List<HNode> Children = new();

    /** parent node. Can be null */
    public readonly HNode? Parent;

    /** Reference to original input string */
    public readonly string Src;

    /** true if the element tag name starts with '_' */
    public bool IsUnderscored;

    /** Parser type of this node */
    public int Type;

    /** start of outer HTML (total span of this node, including children) */
    public int SrcStart;
    /** end of outer HTML (total span of this node, including children) */
    public int SrcEnd;

    /** start of inner HTML (internal span of this node, including children) */
    public int ContStart;
    /** end of inner HTML (internal span of this node, including children) */
    public int ContEnd;

    /** Parse a HTML fragment to a node tree */
    public static HNode Parse(string src) {
        var basis = new HNode(null, src);

        ParseRecursive(basis, src, 0);
        basis.ContStart = basis.SrcStart = 0;
        basis.ContEnd = basis.SrcEnd = src.Length - 1;
        basis.Type = TypeRoot;

        return basis;
    }

    /** Get the text contained by this element, or empty string */
    public string InnerText() {
        try {
            var sb = new StringBuilder();
            foreach (var node in Children) InnerTextRecursive(node, sb);
            return sb.ToString();
        } catch (Exception ex) {
            return "";
        }
    }

    /// <summary>
    /// Render this node and its children as HTML/XML.
    /// The text is recovered from the original input string.
    /// </summary>
    public string ToHtmlString()
    {
        return Src.Substring(SrcStart, 1 + SrcEnd - SrcStart);
    }
    
    public override string ToString(){
        var typeStr = Type switch
        {
            TypeNode => "node",
            TypeElem => "elem",
            TypeSkit => "raw",
            TypeDirk => "directive",
            TypeRoot => "root",
            TypeText => "text",
            _ => "?"
        };
        if (Children.Count < 1) {
            var end = ContEnd + 1;
            if (end > Src.Length) end--;
            if (SrcStart == ContStart) return typeStr+", text: " + Src.Substring(ContStart, end-ContStart);
            return typeStr+", "+Src.Substring(SrcStart, ContStart-SrcStart)+" => '"+Src.Substring(ContStart, end-ContStart)+"'"; // just the opening tag
        }

        return typeStr + ", " + Src.Substring(SrcStart, ContStart-SrcStart) + "; contains " + Children.Count;
    }

    //region internals


    private static void InnerTextRecursive(HNode node, StringBuilder outp) {
        if (node.Children.Count < 1) {
            // not a container. Slap in contents
            outp.Append(node.Src, node.SrcStart, node.SrcEnd + 1);
            return;
        }

        // Opening tag
        if (node.SrcStart < node.ContStart) { // false for comments, scripts, etc
            outp.Append(node.Src, node.SrcStart, node.ContStart);
        }

        // Recurse each child
        foreach (var child in node.Children) InnerTextRecursive(child, outp);

        // Closing tag
        if (node.ContEnd < node.SrcEnd) { // false for comments, scripts, etc
            outp.Append(node.Src, node.ContEnd + 1, node.SrcEnd + 1);
        }
    }

    /** String.indexOf() value for not found */
    private const int NotFound = -1;


    protected HNode(HNode? parent, string src){
        Parent=parent;
        IsUnderscored = false;
        Src = src;
    }
    protected HNode(HNode parent, int srcStart, int contStart, int contEnd, int srcEnd, int type){
        IsUnderscored = false;
        Parent = parent;Src = parent.Src;Type = type;
        SrcStart = srcStart;ContStart = contStart;ContEnd = contEnd;SrcEnd = srcEnd;
    }

    /** recurse, return the offset we ended */
    private static int ParseRecursive(HNode target, string src, int offset){
        var lastIndex = src.Length-1;
        var left = offset;
        target.ContStart = offset;
        while (left <= lastIndex){
            var leftAngle = src.IndexOf('<', left);
            var more = lastIndex - leftAngle;

            if (leftAngle == NotFound){
                // no more markup in the source.
                // if left..end is not empty, add a text node and return
                leftAngle = lastIndex;
                if (leftAngle > left){
                    TextChild(target, left, lastIndex);
                }
                return leftAngle; // return because EOF
            }

            if (leftAngle >= lastIndex) {
                // sanity check: got a '<' as the last character
                TextChild(target, left, lastIndex);
                return lastIndex; // return because it's broken.
            }

            // we have the start of an element <...>
            var rightAngle = src.IndexOf('>', left);

            // sanity checks
            if (rightAngle == NotFound || more < 1 || rightAngle == left + 1) { // `<`...EOF (x2)   or  `<>`
                // invalid markup? Consider the rest as text and bail out
                TextChild(target, left, lastIndex);
                return lastIndex + 1; // return because it's broken.
            }
            switch (src[leftAngle+1])
            {
                case '/':
                {
                    //   </...
                    // If there is any content up to this point, add it as a 'text' child
                    if (leftAngle > left){ //???
                        TextChild(target, left, leftAngle - 1);
                    }

                    // end of our own tag
                    target.SrcEnd = rightAngle;
                    target.ContEnd = leftAngle - 1;
                    if (target.ContEnd == 0) target.ContEnd = -1;
                    // TODO: unwind until we get to a matching tag, to handle bad markup.
                    return rightAngle + 1; // return because it's the end of this tag.
                }
                case '!':
                {
                    // <!...
                    if (more > 2 && src[leftAngle+2] == '-' && src[leftAngle+3] == '-') {
                        // `<!-- ... -->`
                        left = ProcessOtherBlock(target, left, "-->") + 1;
                    } else { // `<!...>`
                        DirectiveChild(target, left, rightAngle);
                        left = rightAngle+1;
                    }
                    continue;
                }
                case '?': // <? ... ?>
                    // this is an XML directive.
                    DirectiveChild(target, left, rightAngle);
                    left = rightAngle+1;
                    continue;
            }

            // a child tag
            // check for 'empty' element
            if (src[rightAngle - 1] == '/') { // goes like <elem ... />
                ElemChild(target, left, rightAngle);
                left = rightAngle + 1;
                continue;
            }

            if (IsScript(target, leftAngle)){
                left = ProcessOtherBlock(target, left, "</script>") + 1;
                continue;
            }
            // This looks like a real node. Recurse

            // If there is any content up to this point, add it as a 'text' child
            if (leftAngle > left){
                TextChild(target, left, leftAngle - 1);
            }

            // Start the child node and recurse it
            var node = new HNode(target, target.Src);
            node.Type = TypeNode;
            node.IsUnderscored = src[leftAngle + 1] == '_';
            node.SrcStart = leftAngle;
            left = ParseRecursive(node, src, rightAngle + 1);
            if (node.ContEnd < 1 || node.SrcEnd < 1){
                // Add logging or reporting here
            }
            else
            {
                target.Children.Add(node);
            }
        }
        return left;
    }

    private static bool IsScript(HNode target, int offset) {
        const string script = "<script";

        if (offset+7 > target.Src.Length) return false;
        var tag = target.Src.Substring(offset, 7);

        return tag.Equals(script);
    }

    private static int ProcessOtherBlock(HNode target, int left, string terminator) {
        var right = target.Src.IndexOf(terminator, left, StringComparison.Ordinal);
        if (right == NotFound) right = target.Src.Length;
        else right += terminator.Length;
        target.Children.Add(new HNode(target, left, right,right,right, TypeSkit));
        return right;
    }

    private static void ElemChild(HNode target, int left, int right) {
        target.Children.Add(new HNode(target, left, right+1, right+1, right, TypeElem));
    }

    private static void DirectiveChild(HNode target, int left, int right) {
        target.Children.Add(new HNode(target, left, right, right, right, TypeDirk));
    }

    private static void TextChild(HNode target, int left, int right) {
        target.Children.Add(new HNode(target, left, left, right, right, TypeText));
    }


    //endregion
}