// OTT Multi-View 서버 (Google Apps Script)
//
// 시트 구성
//   Users   : idx | id | pw | nickname | salt | email
//   OttList : userIdx | name | url | dbIdx
//   Layouts : userIdx | name | slots   (없으면 자동 생성)
//
// 비밀번호: 앱은 SHA-256 해시를 보내고, 서버는 사용자별 salt를 붙여 다시 해시해 저장합니다.
// salt가 없는 기존 계정은 다음 로그인 때 자동으로 바뀝니다.

var ss = SpreadsheetApp.getActiveSpreadsheet();
var userSheet = ss.getSheetByName("Users");
var ottSheet = ss.getSheetByName("OttList");
var layoutSheet = ss.getSheetByName("Layouts") || ss.insertSheet("Layouts");

function text(s) {
  return ContentService.createTextOutput(s);
}

function sha256(s) {
  var bytes = Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, s, Utilities.Charset.UTF_8);
  return bytes.map(function (b) {
    return ("0" + (b & 0xff).toString(16)).slice(-2);
  }).join("");
}

// rowNum = 시트의 실제 행 번호(1부터)
function setPassword(rowNum, clientHash) {
  var salt = Utilities.getUuid();
  userSheet.getRange(rowNum, 3).setValue(sha256(salt + clientHash));
  userSheet.getRange(rowNum, 5).setValue(salt);
}

function checkPassword(row, rowNum, clientHash) {
  var salt = row[4];
  if (salt) return row[2] == sha256(salt + clientHash);
  if (row[2] != clientHash) return false;
  setPassword(rowNum, clientHash);  // 기존 계정을 salt 방식으로 전환
  return true;
}

// 아이디/비밀번호가 맞는 행 번호를 반환 (없으면 -1)
function findLoginRow(data, id, pw) {
  for (var i = 1; i < data.length; i++) {
    if (data[i][1] == id) return checkPassword(data[i], i + 1, pw) ? i + 1 : -1;
  }
  return -1;
}

function deleteRowsWhere(sheet, column, value) {
  var data = sheet.getDataRange().getValues();
  for (var i = data.length - 1; i >= 1; i--) {
    if (data[i][column] == value) sheet.deleteRow(i + 1);
  }
}

// 앱의 비밀번호 규칙(영문+숫자+특수문자 8자 이상)을 만족하는 임시 비밀번호
function tempPassword() {
  return "Ott!" + Utilities.getUuid().replace(/-/g, "").slice(0, 8) + "7a";
}

function doPost(e) {
  var action = e.parameter.action;

  if (action == "login") {
    var data = userSheet.getDataRange().getValues();
    var row = findLoginRow(data, e.parameter.id, e.parameter.pw);
    if (row < 0) return text("-1");
    return text(data[row - 1][0] + "|" + data[row - 1][3]);
  }

  if (action == "register") {
    var data = userSheet.getDataRange().getValues();
    var maxIdx = 0;
    for (var i = 1; i < data.length; i++) {
      if (data[i][1] == e.parameter.id) return text("duplicate");
      maxIdx = Math.max(maxIdx, Number(data[i][0]));
    }
    // 탈퇴로 행이 줄어도 번호가 겹치지 않도록 가장 큰 번호 + 1 사용
    userSheet.appendRow([maxIdx + 1, e.parameter.id, "", e.parameter.nickname]);
    setPassword(userSheet.getLastRow(), e.parameter.pw);
    return text("success");
  }

  if (action == "getList") {
    var data = ottSheet.getDataRange().getValues();
    var list = [];
    for (var i = 1; i < data.length; i++) {
      if (data[i][0] == e.parameter.userIdx) list.push(data[i][3] + "|" + data[i][1] + "|" + data[i][2]);
    }
    return text(list.join(","));
  }

  if (action == "add") {
    ottSheet.appendRow([e.parameter.userIdx, e.parameter.name, e.parameter.url, new Date().getTime().toString()]);
    return text("success");
  }

  if (action == "delete") {
    deleteRowsWhere(ottSheet, 3, e.parameter.dbIdx);
    return text("success");
  }

  if (action == "update") {
    var data = ottSheet.getDataRange().getValues();
    for (var i = 1; i < data.length; i++) {
      if (data[i][3] == e.parameter.dbIdx) {
        ottSheet.getRange(i + 1, 2).setValue(e.parameter.name);
        ottSheet.getRange(i + 1, 3).setValue(e.parameter.url);
        return text("success");
      }
    }
    return text("notFound");
  }

  if (action == "updateUserInfo" || action == "updateNickname" || action == "updatePassword") {
    var rows = userSheet.getDataRange().getValues();
    for (var i = 1; i < rows.length; i++) {
      if (rows[i][0] == e.parameter.userIdx) {
        if (action != "updateNickname") setPassword(i + 1, e.parameter.pw);
        if (action != "updatePassword") userSheet.getRange(i + 1, 4).setValue(e.parameter.nickname);
        return text("success");
      }
    }
    return text("fail");
  }

  if (action == "setEmail") {
    var data = userSheet.getDataRange().getValues();
    var row = findLoginRow(data, e.parameter.id, e.parameter.pw);
    if (row < 0) return text("fail");
    userSheet.getRange(row, 6).setValue(e.parameter.email);
    return text("success");
  }

  if (action == "resetPassword") {
    var data = userSheet.getDataRange().getValues();
    for (var i = 1; i < data.length; i++) {
      if (data[i][1] == e.parameter.id && data[i][5] && data[i][5] == e.parameter.email) {
        var temp = tempPassword();
        setPassword(i + 1, sha256(temp));
        MailApp.sendEmail(e.parameter.email, "OTT Multi-View 임시 비밀번호",
          "임시 비밀번호: " + temp + "\n로그인 후 비밀번호를 변경해주세요.");
        return text("success");
      }
    }
    return text("notFound");
  }

  if (action == "deleteAccount") {
    var data = userSheet.getDataRange().getValues();
    var row = findLoginRow(data, e.parameter.id, e.parameter.pw);
    if (row < 0) return text("fail");
    var idx = data[row - 1][0];
    deleteRowsWhere(ottSheet, 0, idx);
    deleteRowsWhere(layoutSheet, 0, idx);
    userSheet.deleteRow(row);
    return text("success");
  }

  if (action == "saveLayout") {
    var data = layoutSheet.getDataRange().getValues();
    for (var i = 0; i < data.length; i++) {
      if (data[i][0] == e.parameter.userIdx && data[i][1] == e.parameter.name) {
        layoutSheet.getRange(i + 1, 3).setValue(e.parameter.slots);
        return text("success");
      }
    }
    layoutSheet.appendRow([e.parameter.userIdx, e.parameter.name, e.parameter.slots]);
    return text("success");
  }

  // 응답 형식: 한 줄에 "이름<탭>슬롯"
  if (action == "getLayouts") {
    var data = layoutSheet.getDataRange().getValues();
    var list = [];
    for (var i = 0; i < data.length; i++) {
      if (data[i][0] == e.parameter.userIdx) list.push(data[i][1] + "\t" + data[i][2]);
    }
    return text(list.join("\n"));
  }

  return text("unknown");
}
