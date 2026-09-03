package com.smartar.export;

import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentTransaction;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.os.ParcelFileDescriptor;
import com.unity3d.player.UnityPlayer;
import java.io.FileOutputStream;

public class FileExportFragment extends Fragment {
    private static final int CREATE_DOCUMENT_REQUEST = 1001;
    private String csvContent;
    private String callbackGameObject;
    private String callbackMethodSuccess;
    private String callbackMethodFailure;
    private String callbackMethodCancel;

    public static void exportCSV(String content, String defaultFileName, String go, String success, String failure, String cancel) {
        Activity activity = UnityPlayer.currentActivity;
        final FileExportFragment fragment = new FileExportFragment();
        fragment.csvContent = content;
        fragment.callbackGameObject = go;
        fragment.callbackMethodSuccess = success;
        fragment.callbackMethodFailure = failure;
        fragment.callbackMethodCancel = cancel;
        
        Bundle args = new Bundle();
        args.putString("fileName", defaultFileName);
        fragment.setArguments(args);

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                FragmentTransaction transaction = UnityPlayer.currentActivity.getFragmentManager().beginTransaction();
                transaction.add(fragment, "FileExportFragment");
                transaction.commit();
            }
        });
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        String fileName = "SmartARMeasure_Logs.csv";
        if (getArguments() != null) {
            fileName = getArguments().getString("fileName", "SmartARMeasure_Logs.csv");
        }

        Intent intent = new Intent(Intent.ACTION_CREATE_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("text/csv");
        intent.putExtra(Intent.EXTRA_TITLE, fileName);
        startActivityForResult(intent, CREATE_DOCUMENT_REQUEST);
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == CREATE_DOCUMENT_REQUEST) {
            if (resultCode == Activity.RESULT_OK && data != null && data.getData() != null) {
                Uri uri = data.getData();
                try {
                    ParcelFileDescriptor pfd = getActivity().getContentResolver().openFileDescriptor(uri, "w");
                    if (pfd != null) {
                        FileOutputStream fos = new FileOutputStream(pfd.getFileDescriptor());
                        fos.write(csvContent.getBytes("UTF-8"));
                        fos.close();
                        pfd.close();
                        UnityPlayer.UnitySendMessage(callbackGameObject, callbackMethodSuccess, "");
                    } else {
                        UnityPlayer.UnitySendMessage(callbackGameObject, callbackMethodFailure, "");
                    }
                } catch (Exception e) {
                    UnityPlayer.UnitySendMessage(callbackGameObject, callbackMethodFailure, e.getMessage());
                }
            } else {
                UnityPlayer.UnitySendMessage(callbackGameObject, callbackMethodCancel, "");
            }
            
            final Fragment f = this;
            getActivity().runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    getActivity().getFragmentManager().beginTransaction().remove(f).commit();
                }
            });
        }
    }
}
