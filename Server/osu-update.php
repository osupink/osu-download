<?php
require_once('config.php');
header('content-type:text/plain;charset=utf-8');
$officialMirrorHiddenFlag=1;
$officialNoticeList=array('[广告] BanYou 是国内领先的 osu! 第三方服务器，我们只针对长期玩家提供服务，注册十分简单，不仅可以单独游玩，还能邀请好友进行多人游戏，并且拥有独立的排名。立即加入我们的玩家群，群号：686469603');
$mirrorList=array(
	// MirrorURL|MirrorName|MirrorHashCheckFlag (Default: 0, mirror: 0,1,2/officialMirror: 0,1)|MirrorAD|MirrorHiddenFlag (Default: 0, for officialMirror)
	"http://us-la.mirror.osu.pink/|官方 Mirror [美国/洛杉矶]|1||{$officialMirrorHiddenFlag}", // officialMirror
	'https://txy1.sayobot.cn/client/|Sayo Mirror [中国/上海]|0|来自 osu.sayobot.cn 的 Mirror，仅支持 Latest'
);
$payMirrorList=array();
// oom: Only official mirror
if (isset($_GET['oom']) && $_GET['oom'] == "1") {
	die($mirrorList[0]);
}
if (isset($_SERVER['HTTP_USER_AGENT'])) {
	$uaArr=explode('/',$_SERVER['HTTP_USER_AGENT'],2);
	if (count($uaArr) === 2 && (ltrim($uaArr[1],'b') < clientMinVersion)) {
		header('HTTP/1.1 503 Service Unavailable');
		die('客户端版本太老！请前往 https://github.com/osupink/osu-download/releases/ 下载新版本。');
	}
}
// om: Only mirror, p: Pay identity
if (isset($_GET['om']) && $_GET['om'] == "1") {
	foreach ($officialNoticeList as $officialNotice) {
		echo "OfficialNotice:{$officialNotice}\n";
	}
	if (isset($_GET['p']) && $_GET['p'] == "1") {
		foreach ($payMirrorList as $value) {
			echo "PayMirror:{$value}\n";
		}
	}
	foreach ($mirrorList as $value) {
		if ($officialMirrorHiddenFlag !== -1) {
			$officialMirrorHiddenFlag=-1;
			echo "Official";
		}
		echo "Mirror:{$value}\n";
	}
}
?>
