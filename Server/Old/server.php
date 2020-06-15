<?php
error_reporting(0);
set_time_limit(120);
ini_set('default_socket_timeout',30);
ignore_user_abort(1);
define('cachedir','cache');
define('savedir','../osu/osu!/save');
header('content-type:text/plain');
$stream='Stable40';
$streamlist=array('Stable','Stable40','Beta40','CuttingEdge');
$OfficialMirror="OfficialMirror:http://osu.acgvideo.cn/osu!/save/|官方 Mirror|BanYou 是国内领先的 osu! 第三方服务器，我们只针对长期玩家提供服务，注册十分简单，不仅可以单独游玩，还能邀请好友进行多人游戏，并且拥有独立的排名。立即加入我们的玩家群，群号：686469603\n";
$MirrorList=array(
//'https://osu.kj415j45.space/osu!/save/|屙屎汉化组镜像源|由 kj415j45 提供'
);
$PayMirrorList=array(
'http://mirror.osupink.com/|官方付费 Mirror|欢迎您使用官方付费 Mirror'
);
if (isset($_GET['oom']) && $_GET['oom'] == "1") {
	die($OfficialMirror);
}
if (isset($_GET['v'])) {
	if (!(ltrim($_GET['v'],"b") >= 20180706.1)) {
		header('HTTP/1.1 503 Service Unavailable');
		die('客户端版本太老！请前往 https://github.com/osupink/osu-download/releases/ 下载新版本。');
	}
}
if (isset($_GET['om']) && $_GET['om'] == "1") {
	echo $OfficialMirror;
	if (isset($_GET['p']) && $_GET['p'] == "1") {
		foreach ($PayMirrorList as $value) {
			echo "Mirror:{$value}\n";
		}
	}
	foreach ($MirrorList as $value) {
		echo "Mirror:{$value}\n";
	}
	die();
}
if (isset($_GET['s'])) {
	if (in_array($_GET['s'],$streamlist)) {
		$stream=$_GET['s'];
	}
}
$checkfile="requested-{$stream}";
if (file_exists($checkfile) && filectime($checkfile) < time()-600) {
	unlink($checkfile);
}
if (!is_dir(cachedir)) {
	mkdir(cachedir);
}
if (!is_dir(savedir)) {
	mkdir(savedir);
}
if (file_exists($checkfile)) {
	$str=file_get_contents(cachedir.'/'.filectime($checkfile)."/{$stream}.json");
} else {
	$str=file_get_contents("https://osu.ppy.sh/web/check-updates.php?action=check&stream=$stream");
	if (empty($str)) {
		header('HTTP/1.1 503 Service Unavailable');
		die('服务器可能不可用！');
	}
	file_put_contents($checkfile,'1');
	$filetime=filectime($checkfile);
	if (!is_dir(cachedir.'/'.$filetime)) {
		mkdir(cachedir.'/'.$filetime);
	}
	file_put_contents(cachedir.'/'.$filetime."/{$stream}.json",$str);
}
$json=json_decode($str);
if (!$json || count($json) <= 0) {
	header('HTTP/1.1 503 Service Unavailable');
	unlink(cachedir.'/'.filectime($checkfile)."/{$stream}.json");
	unlink($checkfile);
	die('服务器无法解码缓存数据！');
}
$echostr='';
foreach ($json as $value) {
	$filedir="{$value->file_hash}";
	$filepath="{$filedir}/{$value->filename}";
	for ($i=0;$i<30;$i++) {
		if (!file_exists(savedir."/".$filedir."/downloading")) {
			break;
		} elseif ($i == 30) {
			unlink(savedir."/".$filepath);
			unlink(savedir."/".$filedir."/downloading");
			rmdir(savedir."/".$filedir);
		}
		sleep(1);
	}
	if (!file_exists(savedir."/".$filepath)) {
		if (!is_dir(savedir."/".$filedir)) {
			mkdir(savedir."/".$filedir);
		}
		file_put_contents(savedir."/".$filedir."/downloading",1);
		$file=file_get_contents($value->url_full);
		if (!$file) {
			header('HTTP/1.1 503 Service Unavailable');
			$error='服务器无法下载缓存文件！';
		}
		if (!file_put_contents(savedir."/".$filepath,$file)) {
			header('HTTP/1.1 503 Service Unavailable');
			$error='服务器无法保存缓存文件！';
		}
		unlink(savedir."/".$filedir."/downloading");
		if (isset($error)) {
			unlink(savedir."/".$filepath);
			rmdir(savedir."/".$filedir);
			die($error);
		}
	}
	$echostr.="File:$filepath\n";
}
echo $echostr;
?>
