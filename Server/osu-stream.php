<?php
require_once('config.php');
header('content-type:text/plain;charset=utf-8');
$stream='Stable40';
if (isset($_GET['v'])) {
	if (isset($_SERVER['HTTP_USER_AGENT']) && trim(explode('/',$_SERVER['HTTP_USER_AGENT'],1)) >= clientMinVersion) {
		header('HTTP/1.1 503 Service Unavailable');
		die('客户端版本太老！请前往 https://github.com/osupink/osu-download/releases/ 下载新版本。');
	}
}
if (isset($_GET['s'])) {
	if (in_array($_GET['s'], streamList)) {
		$stream=$_GET['s'];
	}
}
$checkFile=cacheDir."/requested-{$stream}";
$jsonFile=cacheDir.'/'.filectime($checkFile)."/{$stream}.json";
if (!file_exists($checkFile) || !file_exists($jsonFile)) {
	header('HTTP/1.1 503 Service Unavailable');
	die('服务器暂无任何缓存，请稍后来看看吧～');
}
$str=file_get_contents($jsonFile);
$json=json_decode($str);
foreach ($json as $value) {
	echo "File:{$value->file_hash}/{$value->filename}\n";
}
?>
