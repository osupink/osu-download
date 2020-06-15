<?php
if (PHP_SAPI !== 'cli') { die(); }
chdir(__DIR__);
require_once('config.php');
set_time_limit(0);
ini_set('default_socket_timeout', 30);
function echoWithDate($str, $dot='.') {
	echo date('[Y-m-d H:i:s] ')."{$str}{$dot}\n";
}
function reportError($str) {
	echoWithDate("Error: {$str}");
}
function reportWarning($str) {
	echoWithDate("Warning: {$str}");
}
if (!is_dir(saveDir)) {
	mkdir(saveDir);
}
if (!is_dir(cacheDir)) {
	mkdir(cacheDir);
}
echoWithDate('Server started','!');
while (true) {
	foreach (streamList as $stream) {
		$isChanged=0;
		$str=file_get_contents("https://osu.ppy.sh/web/check-updates.php?action=check&stream={$stream}");
		if (empty($str)) {
			reportError('Server is unavailable');
			break;
		}
		$json=json_decode($str);
		if (!$json || count($json) <= 0) {
			reportError('Unable to decode data');
			break;
		}
		foreach ($json as $value) {
			$fileDir=$value->file_hash;
			$filePath="{$fileDir}/{$value->filename}";
			if (!file_exists(saveDir."/{$filePath}")) {
				if (!is_dir(saveDir."/{$fileDir}")) {
					mkdir(saveDir."/{$fileDir}");
				}
				for ($i=0;$i<2;$i++) {
					$file=file_get_contents($value->url_full);
					if (!empty($file)) {
						break;
					}
				}
				if (empty($file)) {
					reportWarning("Unable to download {$filePath}");
					continue;
				}
				if (!file_put_contents(saveDir."/{$filePath}", $file)) {
					reportWarning("Unable to save {$filePath}");
					continue;
				}
				if (!$isChanged) {
					$isChanged=1;
					echoWithDate("Checking: {$stream}");
				}
				echoWithDate("Saved: {$filePath}");
			}
		}
		if ($isChanged) {
			$checkFile="requested-{$stream}";
			file_put_contents($checkFile, '1');
			$fileTime=filectime($checkFile);
			if (!is_dir(cacheDir.'/'.$fileTime)) {
				mkdir(cacheDir.'/'.$fileTime);
			}
			file_put_contents(cacheDir.'/'.$fileTime."/{$stream}.json", $str);
			echoWithDate("Checked: {$stream}");
		}
		sleep(10);
	}
	sleep(600);
}
?>
