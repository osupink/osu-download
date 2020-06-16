<?php
if (PHP_SAPI !== 'cli') { die(); }
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
echoWithDate('Server started','!');
while (true) {
	$officialMirror=file_get_contents('https://mirror.osu.pink/osu-update.php?oom=1');
	if (!empty($officialMirror)) {
		$officialMirror=explode('|', str_replace('OfficialMirror:', '', $officialMirror))[0];
		foreach (streamList as $stream) {
			$isChanged=0;
			$str=file_get_contents("https://mirror.osu.pink/osu-update.php?s={$stream}");
			if (empty($str)) {
				reportError('Server is unavailable');
				break;
			}
			$files=array_map(function($value) {return str_replace('File:', '', $value);}, array_filter(explode("\n", $str)));
			foreach ($files as $filePath) {
				list($fileDir, $fileName)=explode('/', $filePath, 2);
				if (!file_exists(saveDir."/{$filePath}")) {
					if (!is_dir(saveDir."/{$fileDir}")) {
						mkdir(saveDir."/{$fileDir}");
					}
					for ($i=0;$i<2;$i++) {
						$file=file_get_contents($officialMirror.$filePath);
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
				echoWithDate("Checked: {$stream}");
			}
		}
	}
	sleep(10);
}
?>
