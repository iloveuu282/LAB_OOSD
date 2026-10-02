"""Dependency-free structural checks. These are NOT C# compilation or SQL execution."""
from pathlib import Path
import re, json, xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
results=[]
def check(name,condition):
 results.append(('PASS ' if condition else 'FAIL ')+name)

def lexical_balance(text):
 # Ignore ordinary/verbatim strings, character literals and both kinds of comments.
 pattern=r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/'
 clean=re.sub(pattern,' ',text)
 stack=[];pairs={')':'(',']':'[','}':'{'}
 for ch in clean:
  if ch in '([{':stack.append(ch)
  elif ch in ')]}':
   if not stack or stack.pop()!=pairs[ch]:return False
 return not stack

ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
project=ET.parse(root/'WinForms/EShopping.csproj')
check('TargetFrameworkVersion v4.7.2',project.find('.//m:TargetFrameworkVersion',ns).text=='v4.7.2')
for source in project.findall('.//m:Compile',ns):
 path=root/'WinForms'/source.attrib['Include'];check('Project source exists '+path.name,path.is_file())
 if path.is_file():check('Lexical delimiter balance '+path.name,lexical_balance(path.read_text(encoding='utf-8')))
refs={x.attrib['Include'] for x in project.findall('.//m:Reference',ns)}
check('DataSetExtensions reference', 'System.Data.DataSetExtensions' in refs)
config=ET.parse(root/'WinForms/App.config');connections=config.findall('.//connectionStrings/add')
check('Distinct shop and external product connections',len(connections)==2 and connections[0].attrib['connectionString']!=connections[1].attrib['connectionString'])
forms=(root/'WinForms/Forms.cs').read_text(encoding='utf-8')
check('No SQL API or query text in Forms',not re.search(r'SqlConnection|SqlCommand|Db\.|repo\.|SELECT\s|INSERT\s|UPDATE\s',forms))
model=ET.parse(root/'UML/EShopping_UML.drawio')
check('16 editable UML pages',len(model.findall('diagram'))==16)
for diagram in model.findall('diagram'):
 cells=diagram.findall('.//mxCell');ids={c.attrib['id'] for c in cells}
 valid=all(c.attrib.get('source') in ids and c.attrib.get('target') in ids for c in cells if 'source' in c.attrib or 'target' in c.attrib)
 check('Valid drawio edge ids '+diagram.attrib['id'],valid)
trace=json.loads((root/'Tests/Traceability.json').read_text(encoding='utf-8'))
check('15 requirements traced',len(trace)==15 and len({r['requirement'] for r in trace})==15)
test_source=(root/'WinForms/SelfTests.cs').read_text(encoding='utf-8')
test_ids=set(re.findall(r'Test\("(UT\d+)',test_source))
check('36 distinct C# Service tests',len(test_ids)==36)
manual=(root/'Tests/TestCases.md').read_text(encoding='utf-8')
manual_ids=set(re.findall(r'^## (MT\d+)',manual,re.M))
check('28 manual scenarios',len(manual_ids)==28)
check('Every FR references valid manual cases',all(set(re.findall(r'MT\d+',r['manual_test']))<=manual_ids for r in trace))
schema=(root/'Database/01_shop_schema.sql').read_text(encoding='utf-8')
check('11 internal SQL tables',len(re.findall(r'CREATE TABLE dbo\.',schema))==11)
external=(root/'Database/04_product_mock.sql').read_text(encoding='utf-8')
check('3 external product SQL tables',len(re.findall(r'CREATE TABLE dbo\.',external))==3)
check('UNIQUE order request constraint',bool(re.search(r'AttemptId uniqueidentifier NOT NULL UNIQUE',schema)))
check('No persisted PAN or CSV fields',not re.search(r'\b(PAN|CSV|CardNumber)\b',schema,re.I))
for i in range(1,6):
 for view in (1,2):check('Product image fixture SP00%d_%d'%(i,view),(root/'WinForms/Images'/('SP00%d_%d.png'%(i,view))).is_file())
check('Report exists',(root/'Report/BaoCao_Lab4_eShopping.docx').is_file())
failed=sum(x.startswith('FAIL') for x in results)
results.insert(0,'STATIC STRUCTURAL CHECKS ONLY — no compilation, runtime or SQL execution.')
results.append('Checks: %d; failed: %d'%(len(results)-1,failed))
results.append('C# UT01–UT36, SQL IT01–IT07 and UI MT01–MT28: NOT RUN in authoring environment.')
out=root/'Tests/StaticChecks.txt';out.write_text('\n'.join(results)+'\n',encoding='utf-8')
print('\n'.join(results))
raise SystemExit(1 if failed else 0)
