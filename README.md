# dicomAnony
dicomAnony는 Dicom 파일의 환자 정보 익명화 작업을 쉽게 할 수 있도록 만들었습니다. DICOM 파일에서 환자의 이름, id를 알아볼 수 없도록 익명화 작업을 할 때 파일을 하나하나 수정하기에는 파일 수가 많아 어려움이 있습니다. dicomAnony는 특정 경로 아래에 있는 dicom 파일을 모두 찾아서 이름과 id를 익명화하고, 환자 정보와 익명정보를 매칭하여 excel 파일로 저장합니다. Nuget 패키지에서 **fo-dicom** 라이브러리와 **Microsoft.Office.Interop.Excel** 라이브러리를 설치하여 사용합니다.
> UI, CLI 없이 프로젝트에서 바로 빌드하여 사용해야 합니다.

## AnonymizedDicom
`MainClass`에서 `Anonimize()`를 호출하는 것으로 프로그램이 실행됩니다.
``` c#
public static void Main(string[] args)
{
    AnonymizedDicom anonimized = new AnonymizedDicom();
    anonimized.Anonimize();
}
```

`AnonymizedDicom` 객체를 생성할 때 `filePath`에서 dicom 파일을 검색합니다.
```C#
private string filepath = "/Users/cskim/Desktop/mcstest/dicomtest/dataset";
private List<FileInfo> fInfos = new List<FileInfo>();

public AnonymizedDicom()
{
    string[] files = Directory.GetFiles(filepath, "*.dcm", SearchOption.AllDirectories);
    fInfos = files.Select(file => new FileInfo(file)).ToList();
}
```

`Anonimize()` 메서드는 dicom 파일을 열어서 원본 데이터들과 익명 값을 excel에 쓰고, 익명화한 dicom 파일을 저장합니다. `DicomFile`은 `Open()`을 통해 dicom 파일을 참조하는 것이 아니라 값으로 복사해옵니다. 별도의 `close` 메서드를 사용하지 않고 `AddOrUpdate()`로 수정된 dicom 파일을 `Save()`를 이용해 직접 저장합니다. 따로 저장하지 않으면 변경 사항이 적용되지 않습니다.
``` C#
fInfos = fInfos.Select((info, index) =>
{
    DicomFile dicom = DicomFile.Open(info.FullName);
    string ID = dicom.GetDicomValue(DicomTag.PatientID);
    string name = dicom.GetDicomValue(DicomTag.PatientName);
    string birth = dicom.GetDicomValue(DicomTag.PatientBirthDate);
    //string anony = hashing(ID, name);         
    string anony = CreateAnonymousCode(birth);

    ws.Cells[index + 2, 1] = anony;
    ws.Cells[index + 2, 2] = ID;
    ws.Cells[index + 2, 3] = name.DecodeKR();

    // 이름, ID 익명화
    dicom.Dataset.AddOrUpdate(DicomTag.PatientID, $"AN_ID_{anony}");
    dicom.Dataset.AddOrUpdate(DicomTag.PatientName, $"AN_NM_{anony}");

    // 플래그를 사용하여 덮어쓰거나 새로운 경로에 저장하거나 선택
    if (isOverlap)
    {
        dicom.Save(info.FullName);
    }
    else
    {
        string newpath = filepath + "/anonymous";
        if (!Directory.Exists(newpath))
            newpath = Directory.CreateDirectory(filepath + "/anonymous").FullName;
        newpath += $"/AN_{anony}.dcm";
        dicom.Save(newpath);
    }

    return info;
}).ToList();
```

익명화에 사용되는 값은 해시 값을 사용하거나 환자 생일을 이용해 생성하는 방법이 있습니다. 여기서는 생일을 이용해 값을 생성하고 있습니다.
```C#
// {birthday}{current time}으로 익명화
private string CreateAnonymousCode(string arg)
{
    return $"{arg}{DateTime.Now.ToString("hhmmss")}";
}

// ID와 Name을 이용한 HASH값으로 익명화
private string hashing(string id, string name)
{
    byte[] result;

    byte[] msg_buffer = new ASCIIEncoding().GetBytes(id);
    byte[] key_buffer = new ASCIIEncoding().GetBytes(name);

    HMACSHA1 h = new HMACSHA1(key_buffer);

    result = h.ComputeHash(msg_buffer);

    return Convert.ToBase64String(result);
}
```
> 참고: C#에서 SHA Hash function 사용(http://blog.naver.com/PostView.nhn?blogId=devace&logNo=20063698534)

`isOverlap` 속성을 사용해서 익명화한 dicom 파일을 원본 파일에 덮어쓸지 새로운 경로에 저장할지 결정하도록 했습니다. UI를 구성하게 되면 `isOverlap` 속성을 이용해 옵션으로 사용할 수 있습니다.
```C#
// 플래그를 사용하여 덮어쓰거나 새로운 경로에 저장하거나 선택
if (isOverlap)
{
    dicom.Save(info.FullName);
}
else
{
    string newpath = filepath + "/anonymous";
    if (!Directory.Exists(newpath))
        newpath = Directory.CreateDirectory(filepath + "/anonymous").FullName;
    newpath += $"/AN_{anony}.dcm";
    dicom.Save(newpath);
}
```

## Helper
한글 이름이 깨져있는 dicom 파일이 상당 수 존재합니다. `string`의 extension으로 `DecodeKR()` 함수를 사용하면 깨진 이름을 한글로 변환할 수 있습니다.
```c#
// 한글 디코딩
public static string DecodeKR(this string name)
{
    Encoding iso = Encoding.GetEncoding("ISO-8859-1");
    Decoder euckr = Encoding.GetEncoding(51949).GetDecoder();
    byte[] isoByte = iso.GetBytes(name);
    char[] decodename;
    int charCount = euckr.GetCharCount(isoByte, 0, isoByte.Length);
    decodename = new char[charCount];
    int charDecodedCount = euckr.GetChars(isoByte, 0, isoByte.Length, decodename, 0);
    return new string(decodename);
}
```

dicom 파일을 열면 `Dataset` 속성에서 `GetValue()` 메서드로 태그에 해당하는 환자 정보 값을 불러올 수 있는데, 값이 입력되어 있지 않으면 nullException이 발생합니다. `DicomFile`의 extension인 `GetDicomValue` 메서드를 사용하면 값이 있을 때 가져오고 없는 경우 빈 문자열을 반환하도록 했습니다.
```c#
// Dicom Dataset에서 TAG에 해당하는 값 가져옴
public static string GetDicomValue(this DicomFile dicom, DicomTag tag)
{
    try
    {
        return dicom.Dataset.GetValue<string>(tag, 0);
    }
    catch
    {
        // 태그에 해당하는 값이 없을 떄 빈 string 반환
        return string.Empty;
    }
}
```
