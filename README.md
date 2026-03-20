# WfWattch2Mqtt
ラトック　RS-WWATTCH2を利用したHA向けモニター

<a href="https://github.com/usausa" targert="blank">うさうさ様</a>の<a href="https://github.com/usausa/devicelib-wfwattch2" targer="blank">devicelib-wfwattch2</a>を利用した
HomeAssistantのIntegration Pluginになります。

実行イメージはこのような感じになります。
※ここでは3個のKS-WFWATTCH2からデータを取得しています。
<img width="1230" height="559" alt="Image" src="https://github.com/user-attachments/assets/1caca011-b843-4fd1-859e-424c4843c69e" />

Install方法としては、以下の手順で実施しました。<br/>
<ol>
  <li>HomeAssiostansceに.Net実行環境を用意する（Ver10推奨）</li>
  <li>ビルド済みオブジェクトを対象となるHomeAssistanceに置</li>
  <li>credential.jsonを必要に応じて変更する</li>
  <li>Netが起動したら、自動的にオブジェクトが生成されるので待つ</li>
  <li>Entityとして表示されることを確認する</li>
</ol>

<b><ul>注意事項としては、HomeAssistanceに同一LAN上にない場合、HomeAssistanceにWiFiアダプタがあった方がいいです。</b></ul>

