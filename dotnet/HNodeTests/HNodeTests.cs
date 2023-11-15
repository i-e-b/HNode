using HNodeDotnet;
using NUnit.Framework;

namespace HNodeTests;

[TestFixture]
public class HNodeTests
{
   [Test]
   public void can_read_simple_html4_document()
   {
      var doc = HNode.Parse(SimpleHtmlDoc);
      Assert.That(doc, Is.Not.Null, "result should not be null");
      Assert.That(doc.Type, Is.EqualTo(HNode.HNodeType.Root), "result node should be the document root");
      DumpNodesRec(doc, 0);
      
      var result = doc.SourceString();
      Console.WriteLine(result);
      Assert.That(result, Is.EqualTo(SimpleHtmlDoc), "root node should encompass entire document");
   }
   
   [Test]
   public void can_read_simple_html5_document()
   {
      var doc = HNode.Parse(ModernHtmlDoc);
      Assert.That(doc, Is.Not.Null, "result should not be null");
      Assert.That(doc.Type, Is.EqualTo(HNode.HNodeType.Root), "result node should be the document root");
      DumpNodesRec(doc, 0);
      
      var result = doc.SourceString();
      Console.WriteLine(result);
      Assert.That(result, Is.EqualTo(ModernHtmlDoc), "root node should encompass entire document");
   }

   [Test]
   public void can_read_a_simple_fully_populated_xml_file()
   {
      var doc = HNode.Parse(SimpleXmlFile);
      Assert.That(doc, Is.Not.Null, "result should not be null");
      Assert.That(doc.Type, Is.EqualTo(HNode.HNodeType.Root), "result node should be the document root");
      DumpNodesRec(doc, 0);
      
      var result = doc.SourceString();
      Console.WriteLine(result);
      Assert.That(result, Is.EqualTo(SimpleXmlFile), "root node should encompass entire document");
   }

   [Test]
   public void can_read_a_simple_xml_fragment()
   {
      var doc = HNode.Parse(SimpleXmlFragment);
      Assert.That(doc, Is.Not.Null, "result should not be null");
      Assert.That(doc.Type, Is.EqualTo(HNode.HNodeType.Root), "result node should be the document root");
      DumpNodesRec(doc, 0);
      
      var result = doc.SourceString();
      Console.WriteLine(result);
      Assert.That(result, Is.EqualTo(SimpleXmlFragment), "root node should encompass entire document");
   }

   [Test]
   public void can_get_text_from_nodes()
   {
      var doc = HNode.Parse(SimpleHtmlDoc);
      Assert.That(doc, Is.Not.Null, "result should not be null");
      Assert.That(doc.Type, Is.EqualTo(HNode.HNodeType.Root), "result node should be the document root");
      DumpNodesRec(doc, 0);
      
      Console.WriteLine(doc.InnerText());
      Assert.That(doc.InnerText(), Is.EqualTo("A very simple documentDocumentThis is a simple sample"), "should get only content text");
   }


   [Test]
   public void can_find_elements_within_a_tree()
   {
      var doc = HNode.Parse(ModernHtmlDoc); // HTML5 is harder to parse than HTML4, due to the unclosed 'elements', like <meta charset="utf-8">
      
      // TODO: Need to check that the <head><...><...></head> pattern works (not good XML)
      DumpNodesRec(doc, 0);
      Assert.Inconclusive();
   }

   private static void DumpNodesRec(HNode node, int depth)
   {
      Console.Write(new string(' ',depth*2));
      if (node.Errors) Console.Write("[ERR] ");
      
      switch (node.Type)
      {
         case HNode.HNodeType.Root:
            Console.WriteLine("Root");
            break;
         case HNode.HNodeType.Text:
            Console.WriteLine($"Content: '{OneLine(node.Src.Substring(node.ContStart, node.ContEnd - node.ContStart + 1))}'");
            break;
         case HNode.HNodeType.Node:
            Console.WriteLine($"Node: '{node.Src.Substring(node.SrcStart, node.ContStart - node.SrcStart)}' -> '{node.Src.Substring(node.ContEnd+1, node.SrcEnd - node.ContEnd)}'");
            break;
         case HNode.HNodeType.Element:
            Console.WriteLine($"Element: '{node.Src.Substring(node.SrcStart, node.ContStart - node.SrcStart)}'");
            break;
         case HNode.HNodeType.Directive:
            Console.WriteLine($"Directive: '{node.Src.Substring(node.SrcStart, node.ContStart - node.SrcStart+1)}'");
            break;
         case HNode.HNodeType.CommentOrScript:
            Console.WriteLine($"Script or comment: '{node.Src.Substring(node.SrcStart, node.ContStart - node.SrcStart)}'");
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      foreach (var child in node.Children)
      {
         DumpNodesRec(child, depth+1);
      }
   }

   private static string OneLine(string s) => s.Replace("\r","").Replace("\n","");

   private const string SimpleHtmlDoc = "<html><head><title>A very simple document</title></head><body><h1>Document</h1><p>This is a simple sample</p></body></html>";
   private const string ModernHtmlDoc = "<!DOCTYPE html>\n<html lang=\"en\">\n\n<head>\n  <meta charset=\"utf-8\">\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
                                        "\n  <title>HTML5 Boilerplate</title>\n  <link rel=\"stylesheet\" href=\"styles.css\">\n</head>\n\n<body>\n  <h1>Page Title</h1>" +
                                        "\n  <p>Hello, world</p>\n  <script src=\"scripts.js\"></script>\n</body>\n\n</html>";
   private const string SimpleXmlFile = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<body>\n<message type=\"warning\"><![CDATA[A problem occurred (&quot;Sample&quot;)]]></message>\n</body>";
   private const string SimpleXmlFragment = "<UndeclaredNamespace:Message type=\"warning\">A problem occurred (&quot;Sample&quot;)</UndeclaredNamespace:Message>";
}