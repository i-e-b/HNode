using HNodeDotnet;
using NUnit.Framework;

namespace HNodeTests;

[TestFixture]
public class HNodeTests
{
   [Test]
   public void can_read_html_document()
   {
      var doc = HNode.Parse(SimpleDoc);
      Assert.That(doc, Is.Not.Null);
      
      var result = doc.ToHtmlString();
      Assert.That(result, Is.EqualTo(SimpleDoc));
   }

   private const string SimpleDoc = "<html><head><title>A very simple document</title></head><body><h1>Document</h1><p>This is a simple sample</p></body></html>";
}